# Build stage: restore first (cached while only source files change), then publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Quotes.Domain/Quotes.Domain.csproj src/Quotes.Domain/
COPY src/Quotes.Application/Quotes.Application.csproj src/Quotes.Application/
COPY src/Quotes.Infrastructure/Quotes.Infrastructure.csproj src/Quotes.Infrastructure/
COPY src/Quotes.Api/Quotes.Api.csproj src/Quotes.Api/
RUN dotnet restore src/Quotes.Api/Quotes.Api.csproj

COPY src/ src/
RUN dotnet publish src/Quotes.Api/Quotes.Api.csproj --configuration Release --no-restore --output /app/publish

# Runtime stage: ASP.NET runtime only, running as the image's non-root "app" user on port 8080.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Quotes.Api.dll"]
