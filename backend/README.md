# Advisor Quotes: backend

The quotes API: create a quote with a server-calculated premium, list and view quotes, change a quote's status with a full history, and handle duplicate submissions. .NET 10, ASP.NET Core controllers and PostgreSQL 17, with clean architecture, CQRS, an aggregate root, a transactional outbox, and OpenTelemetry traces, metrics and structured logs.

The project overview and how the backend fits with the UI are in the [repository README](../README.md). Decisions and the AWS plan are in [SOLUTION.md](../SOLUTION.md).

## Prerequisites

| Tool | Needed for | Check |
|---|---|---|
| Docker (Docker Desktop or Rancher Desktop) | PostgreSQL, migrations, the API container, and the integration tests | `docker version` |
| .NET 10 SDK | running from source and the tests | `dotnet --version` |

## Run it locally

`docker compose` runs from the **repository root**, where `docker-compose.yml` and `.env` live. `dotnet` commands run from this **`backend/`** folder.

### Option 1: backend in Docker (simplest)

```bash
# from the repository root
cp .env.example .env                 # once
docker compose up --build api        # PostgreSQL, migrations (grate), then the API
```

- API: http://localhost:8080/api/v1/quotes
- API reference (Scalar): http://localhost:8080/scalar
- Readiness: http://localhost:8080/health/ready
- Metrics: http://localhost:8080/metrics

### Option 2: API from source, for debugging

```bash
# from the repository root: just the database and its migrations
docker compose up -d postgres db-migrations

# from backend/
dotnet run --project src/Quotes.Api --launch-profile http     # http://localhost:5039
```

You can also open `AdvisorQuotes.slnx` in Visual Studio or Rider and run `Quotes.Api` with the `http` profile.

To use the UI with it, run `npm run dev:source` in `frontend/` (not `npm run dev`, which targets the Docker API on 8080).

### Option 3: without Docker

Use any PostgreSQL 17 and apply the schema with grate as a .NET tool:

```powershell
# from backend/
./scripts/migrate.ps1 -ConnectionString "Host=localhost;Port=5432;Database=advisor_quotes;Username=...;Password=..."
$env:ConnectionStrings__QuotesDb = "Host=localhost;Port=5432;Database=advisor_quotes;Username=...;Password=..."
dotnet run --project src/Quotes.Api --launch-profile http
```

## Try it

`src/Quotes.Api/Quotes.Api.http` walks through the whole demo flow: create a quote, retry it (same quote back), reuse the key with a different body (rejected), view it, move it through its statuses, page through the list, and check health and metrics. Set `@baseUrl` to `http://localhost:8080` for Docker or `http://localhost:5039` from source. The Scalar UI at `/scalar` does the same in a browser.

Every response carries `X-Trace-Id`. Search for it in the logs (`docker compose logs api`) or in Jaeger to follow a request end to end, including the outbox publish that happens afterwards in the background.

## Tests

```bash
# from backend/
dotnet test                                              # 156 tests: 86 unit, 9 architecture, 61 integration
dotnet test tests/Quotes.UnitTests                       # fast, no Docker needed
dotnet format AdvisorQuotes.slnx --verify-no-changes     # formatting, as CI checks it
```

The integration tests start their own PostgreSQL container (Testcontainers), so Docker must be running. They don't need the compose stack.

## Layout

```
backend/
├── AdvisorQuotes.slnx           solution (folders 0. Tests … 5. Shared)
├── src/
│   ├── Quotes.Api               1. Presentation    controllers, versioning, rate limiting, problem details, telemetry
│   ├── Quotes.Infrastructure    2. Infrastructure  EF Core + PostgreSQL, outbox dispatcher, maintenance job
│   ├── Quotes.Application       3. Application     use cases, validation, idempotency, ports
│   ├── Quotes.Domain            4. Domain          quote aggregate, status rules, pricing, domain events
│   └── dbQuotes                 5. Shared          grate schema scripts (up/0001 … 0008)
├── tests/                       0. Tests           unit, architecture and integration tests
├── build/                       Dockerfile, Mermaid diagram generation
├── scripts/                     migrate.ps1 (grate without Docker)
└── docs/                        backend documentation and diagrams
```

## Documentation

| Document | Covers |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Layers, request flows, CQRS, the aggregate root, domain events and the outbox |
| [docs/api.md](docs/api.md) | Endpoints, headers, examples, paging, every error code |
| [docs/operations.md](docs/operations.md) | Configuration, health checks, traces, logs, metrics, the outbox runbook, migrations |
| [docs/local-development.md](docs/local-development.md) | Running, testing, reproducing CI, regenerating diagrams, troubleshooting |
| [docs/ai-usage.md](docs/ai-usage.md) | How AI assistance was used |
