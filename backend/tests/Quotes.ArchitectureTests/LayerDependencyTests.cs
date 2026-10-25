using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quotes.Api.Controllers;
using Quotes.Application.Abstractions;
using Quotes.Application.Handlers;
using Quotes.Application.Observability;
using Shouldly;

namespace Quotes.ArchitectureTests
{
    /// <summary>
    /// The onion rule on the compiled assemblies: a layer may only reference its own layer and the layers inside it
    /// (Api → Infrastructure → Application → Domain), and the core layers stay free of framework code.
    /// </summary>
    public class LayerDependencyTests
    {
        private const string SolutionPrefix = "Quotes.";

        private const int MaxHandlerDependencies = 7;

        /// <summary>
        /// Outermost first; the index is the layer's depth.
        /// </summary>
        private static readonly string[] Layers = ["Api", "Infrastructure", "Application", "Domain"];

        private static readonly Assembly[] SolutionAssemblies = LoadSolutionAssemblies();

        public static TheoryData<string> AllLayers => new TheoryData<string>(Layers);

        [Fact]
        public void EverySolutionAssembly_BelongsToALayer()
        {
            SolutionAssemblies.Length.ShouldBe(Layers.Length);
            SolutionAssemblies.ShouldAllBe(assembly => GetLayer(assembly.GetName().Name!) != null);
        }

        [Theory]
        [MemberData(nameof(AllLayers))]
        public void Layer_ReferencesOnlyItselfAndInnerLayers(string layer)
        {
            int depth = Array.IndexOf(Layers, layer);

            List<string> violations = SolutionAssemblies
                .Where(assembly => GetLayer(assembly.GetName().Name!) == layer)
                .SelectMany(assembly => assembly.GetReferencedAssemblies()
                    .Select(reference => reference.Name!)
                    .Where(reference => GetLayer(reference) is string referencedLayer && Array.IndexOf(Layers, referencedLayer) < depth)
                    .Select(reference => $"{assembly.GetName().Name} -> {reference}"))
                .ToList();

            violations.ShouldBeEmpty();
        }

        [Fact]
        public void Domain_ReferencesNoFrameworkOrLibrary()
        {
            List<string> references = ReferencedNames("Domain");

            references.ShouldNotContain(name => name.StartsWith("Microsoft.", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("Npgsql", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("FluentValidation", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("Serilog", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("OpenTelemetry", StringComparison.Ordinal));
        }

        [Fact]
        public void Application_ReferencesNoPersistenceOrWebFramework()
        {
            List<string> references = ReferencedNames("Application");

            references.ShouldNotContain(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("Npgsql", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("Serilog", StringComparison.Ordinal));
            references.ShouldNotContain(name => name.StartsWith("OpenTelemetry", StringComparison.Ordinal));
        }

        [Fact]
        public void Controllers_DependOnlyOnApplicationAbstractions()
        {
            Assembly api = typeof(ApiControllerBase).Assembly;

            List<string> violations = api.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .SelectMany(controller => controller.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Where(parameter => !IsApplicationAbstraction(parameter.ParameterType))
                    .Select(parameter => $"{controller.Name}({parameter.ParameterType.Name} {parameter.Name})"))
                .ToList();

            violations.ShouldBeEmpty();
        }

        [Fact]
        public void UseCaseHandlers_StayFocused_AndLeaveObservabilityToDecorators()
        {
            Assembly application = typeof(CreateQuoteHandler).Assembly;
            Type[] handlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

            List<Type> handlers = application.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false }
                    && type.Namespace == typeof(CreateQuoteHandler).Namespace
                    && type.GetInterfaces().Any(contract => contract.IsGenericType
                        && handlerInterfaces.Contains(contract.GetGenericTypeDefinition())))
                .ToList();

            handlers.ShouldNotBeEmpty();

            foreach (Type handler in handlers)
            {
                ParameterInfo[] dependencies = handler.GetConstructors().Single().GetParameters();

                dependencies.Length.ShouldBeLessThanOrEqualTo(MaxHandlerDependencies, $"{handler.Name} has too many responsibilities");
                dependencies.ShouldNotContain(
                    parameter => parameter.ParameterType == typeof(QuotesMetrics)
                        || (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>)),
                    $"{handler.Name} should leave metrics and logging to its Instrumented decorator");
            }
        }

        private static bool IsApplicationAbstraction(Type type)
        {
            return type.IsInterface && type.Assembly.GetName().Name == SolutionPrefix + "Application";
        }

        private static List<string> ReferencedNames(string layer)
        {
            return SolutionAssemblies
                .Single(assembly => GetLayer(assembly.GetName().Name!) == layer)
                .GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .ToList();
        }

        private static string? GetLayer(string assemblyName)
        {
            if (!assemblyName.StartsWith(SolutionPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            string layer = assemblyName[SolutionPrefix.Length..];

            return Layers.Contains(layer) ? layer : null;
        }

        private static Assembly[] LoadSolutionAssemblies()
        {
            string testAssemblyName = typeof(LayerDependencyTests).Assembly.GetName().Name!;

            return Directory.GetFiles(AppContext.BaseDirectory, $"{SolutionPrefix}*.dll")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != testAssemblyName)
                .Select(name => Assembly.Load(new AssemblyName(name!)))
                .ToArray();
        }
    }
}
