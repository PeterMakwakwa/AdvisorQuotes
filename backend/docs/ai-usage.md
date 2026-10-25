# AI usage

## Summary

This solution was built with substantial help from an AI coding assistant, **Claude Code** (Anthropic, Claude Opus 5.5), working in my local repository. I set the direction and the engineering standards, made the design decisions, and reviewed and verified the result. The assistant wrote most of the code, tests and documentation at my request.

I worked with the assistant like a fast pair programmer: I gave it the architecture and standards up front (through my own skill and my team's conventions), built the solution step by step, and reviewed each step before moving on. When something didn't meet my standards, I asked for it to be compared, questioned or reworked, and I kept the final say on every design decision.

## Scaffolding

I scaffolded the backend using my own AI skill, **`architectural-patterns`**: a reusable instruction set I maintain that tells the assistant **when and why** to apply each pattern (implementation details live in my separate coding-standards skill). I referenced it manually while directing the work, and it shaped the architecture of this solution:

| Pattern in my skill | Rule in the skill | Where it shows up here |
|---|---|---|
| SOLID | Non-negotiable, every class | Focused handlers, an extracted idempotency guard, decorators for observability, everything injected through interfaces |
| Clean Architecture | Non-negotiable, strict layer separation | Domain → Application → Infrastructure → Api, enforced by architecture tests |
| CQRS | When read and write shapes differ | Command handlers on the aggregate; queries read projections directly |
| Aggregate Root | When entities must stay consistent as a group | `Quote` owns its status history and raises domain events |
| Repository + Unit of Work | Always with a database | `IQuoteRepository`, `IUnitOfWork` (one transaction per use case) |
| Idempotency | Non-negotiable for state-changing API requests | `Idempotency-Key` header and body fingerprint, `idempotency_keys` table |
| Event-Driven + Transactional Outbox | When events must be atomic with database writes | `outbox_messages`, written in the same transaction and published by a background dispatcher |
| Observability | Non-negotiable, three pillars | Structured logs, OpenTelemetry metrics and traces, linked by the trace id |
| Factory | When construction is non-trivial | `Quote.Create`, the quote reference generator |

Medallion Architecture is also in the skill but doesn't apply here, because there is no multi-stage data processing.

## What I decided and directed

These choices came from me, based on the brief and on the conventions I use at work:

- **Scope:** the backend first; the React UI was added afterwards as a separate app in `frontend/`; authentication is out of scope.
- **Stack and style:** .NET 10, ASP.NET Core **controllers**, PostgreSQL; the code style of my current team's services (block-scoped namespaces, explicit types, `private set` / `init`, command records with `BuildCommand()`, `.editorconfig`).
- **Clean architecture** with **SQL-first migrations using grate** and **docker compose**, as in my team's payment service.
- **Libraries:** public NuGet only, based on a previous project of mine (FluentValidation, Serilog); no internal company packages.
- **Tests:** a test suite was a requirement.
- **Solution layout:** the same structure as a previous solution of mine (numbered solution folders, SQL scripts as a shared project, `build/` folder).
- **Iterative reviews I asked for**, each of which led to changes:
  - custom business metrics, in addition to the tracing signal
  - checking the requirements against the brief (which added the optional adviser reference and migrations without Docker)
  - checking CQRS and clean architecture (a proper list query, a true read-side projection, aggregate root guard tests)
  - comparing with my previous solution (CI, formatting, request normalisation, architecture tests for every layer, versioning, rate limiting, `/metrics`, cursor paging, the outbox)
  - removing database locks while publishing (claim-with-lease)
  - batched cleanup, outbox health and backlog metrics, this documentation and the diagram generation

I also made these design decisions myself:

- **How to implement CQRS:** commands go through the aggregate and its repository; queries read projections directly.
- **grate rather than Entity Framework migrations:** EF Core is used for data access only, and the schema is owned by versioned SQL scripts.
- **Controllers rather than minimal APIs:** `ControllerBase` controllers that only call Application handlers.
- **FluentValidation** for request validation.
- **An onion layout within my clean architecture,** with dependencies pointing inwards to the domain, for a clearer layout and better readability.
- **The transactional outbox,** so events are saved in the same transaction as the quote change.
- **Jaeger for tracing,** to view the OpenTelemetry traces locally, including the outbox publish in the same trace as the request.

## What the assistant did

- Proposed the overall plan and design options, and explained the trade-offs.
- Wrote the production code, the SQL migration scripts, the tests, the CI workflow, the Docker setup and the documentation.
- **All of the unit tests were generated with AI** (the 85 tests in `Quotes.UnitTests`: domain rules, aggregate guards, pricing, the idempotency guard, handlers, paging and metrics). I decided what had to be covered and reviewed the generated tests.
- Ran the builds, tests and the local stack, and fixed what failed.

## How the output was verified

- **156 automated tests:** unit, architecture and integration against real PostgreSQL (Testcontainers), including concurrency tests (parallel duplicate submissions, parallel status changes, parallel outbox dispatchers).
- **Guard tests checked by deliberately breaking the code:** a public setter on the aggregate, and a lock held while publishing; both were caught.
- **Every CI step run locally:** formatting, a warnings-as-errors Release build, tests with coverage, the vulnerable-package audit, and the Docker build.
- **The running system checked end to end:** the demo flow against the containerised API, the traces in Jaeger (including the outbox publish span inside the request's trace), the Prometheus metrics, rate limiting, migrations from an empty database.

I also validated it myself:

- I ran the code locally, starting the stack with docker compose.
- I tested everything manually: creating quotes, duplicate submissions, status changes, the list, and the logs and traces.
- I tested the integration with the UI application end to end.

## My understanding

I can explain and defend every design decision in this solution. With more time, I would add:

- **Event streaming for other departments.** When other departments need to consume our quotes, they would get them through **Kafka** (for example Amazon MSK). The outbox is already in place, so this means adding a Kafka publisher behind `IIntegrationEventPublisher`, without changing the use cases.
- **JWT authentication and authorisation,** so the adviser and the person changing a status come from the token instead of the request.
- **Secrets in a vault.** Passwords and other secrets, such as the database connection string, should be handled by a secrets manager like **HashiCorp Vault** or **AWS Secrets Manager**, not by configuration files. Today the only password is a local development placeholder in `.env` and `appsettings.Development.json`; in other environments the app would read its secrets from the vault at startup, and the vault would rotate them.
- **Separate middleware for cross-cutting concerns,** to keep the controllers and startup code leaner and smaller.
