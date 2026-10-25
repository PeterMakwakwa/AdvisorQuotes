# Local development

**On this page:** [Tools](#tools) · [Run it](#run-it) · [Tests and quality gates](#tests-and-quality-gates) · [Diagrams](#diagrams) · [Observability locally](#observability-locally) · [Troubleshooting](#troubleshooting)

## Tools

| Tool | Needed for |
|---|---|
| **Docker** (Docker Desktop or Rancher Desktop) | PostgreSQL, migrations, the API container, Jaeger, and Testcontainers in the integration tests |
| **.NET 10 SDK** | running from source and the tests |
| **Visual Studio 2022+, Rider or VS Code** | editing; the solution groups projects as `0. Tests` … `5. Shared` |
| **Node.js + Mermaid CLI** (optional) | regenerating the diagram PNGs: `npm install -g @mermaid-js/mermaid-cli` |

Local .NET tools are pinned in `dotnet-tools.json` (run `dotnet tool restore` once): **grate** (migrations without Docker) and **dotnet-counters** (live metrics).

## Run it

`docker compose` commands run from the **repository root**, where `docker-compose.yml` and `.env` live. `dotnet` commands run from **`backend/`**, where the solution is.

**Everything in Docker** (database, migrations, API and the UI):

```bash
cp .env.example .env
docker compose up --build                     # API on http://localhost:8080, UI on http://localhost:3000
```

**API from source** (debugging), database from compose:

```bash
docker compose up -d postgres db-migrations                  # from the repository root
cd backend
dotnet run --project src/Quotes.Api --launch-profile http    # http://localhost:5039
```

For the UI against this API, run `npm run dev:source` in `frontend/` (it forwards `/api` to port 5039; `npm run dev`
targets the Docker API on 8080).

**Without Docker** (any PostgreSQL):

```powershell
cd backend
./scripts/migrate.ps1 -ConnectionString "Host=localhost;Port=5432;Database=advisor_quotes;Username=...;Password=..."
$env:ConnectionStrings__QuotesDb = "Host=localhost;Port=5432;Database=advisor_quotes;Username=...;Password=..."
dotnet run --project src/Quotes.Api --launch-profile http
```

Then use `src/Quotes.Api/Quotes.Api.http` (Visual Studio, Rider or the VS Code REST Client) or the Scalar UI at `/scalar`.

## Tests and quality gates

From `backend/`:

```bash
dotnet test                                   # unit, architecture and integration (Docker must be running)
dotnet test tests/Quotes.UnitTests            # fast, no Docker
dotnet format AdvisorQuotes.slnx --verify-no-changes
```

To reproduce CI exactly:

```bash
dotnet restore AdvisorQuotes.slnx
dotnet format AdvisorQuotes.slnx --verify-no-changes --no-restore
dotnet build AdvisorQuotes.slnx -c Release --no-restore
dotnet test AdvisorQuotes.slnx -c Release --no-build --collect "XPlat Code Coverage"
dotnet list AdvisorQuotes.slnx package --vulnerable --include-transitive
docker build -f build/docker/Dockerfile .     # build context is backend/
(cd .. && docker compose config --quiet)
```

The integration tests start their own PostgreSQL container (Testcontainers), apply `src/dbQuotes/up`, and run the API in memory with the background jobs switched off and the broker replaced by a recording publisher, so outbox behaviour can be asserted exactly.

## Diagrams

The diagrams in `docs/` are PNGs **generated from** the `.mermaid` files next to them; edit the `.mermaid` file, never the PNG.

- **Automatically:** a Debug build of `Quotes.Api` on Windows re-renders any diagram whose `.mermaid` file is newer than its `.png` (the `GenerateMermaidDiagrams` target; MSBuild skips it when everything is up to date). It never runs in Release, CI or Docker builds, and never fails a build. Without the Mermaid CLI it prints a message and does nothing.
- **By hand:** `./build/mermaid/generate-mermaid-png.ps1` (changed diagrams) or `-Force` (all of them).
- Commit the `.mermaid` source and the regenerated `.png` together.

## Observability locally

| What | How |
|---|---|
| Traces in the console | default (`Telemetry__ConsoleExporter=true`) |
| Traces in Jaeger | uncomment the three Jaeger lines in `.env` (`OTEL_EXPORTER_OTLP_ENDPOINT`, `COMPOSE_PROFILES=observability`, `TELEMETRY_CONSOLE_EXPORTER=false`), `docker compose up --build`, open http://localhost:16686 |
| Metrics | `curl http://localhost:8080/metrics`, or `dotnet tool run dotnet-counters monitor --name Quotes.Api --counters Quotes.Application,Quotes.Infrastructure.Messaging` (from source) |
| Outbox | `docker compose exec postgres psql -U advisor_quotes -d advisor_quotes -c "SELECT event_type, attempts, processed_at FROM outbox_messages ORDER BY occurred_at DESC LIMIT 10"` |
| Health | `curl http://localhost:8080/health` |

## Troubleshooting

| Problem | Fix |
|---|---|
| `failed to connect to the docker API` | start Docker / Rancher Desktop |
| Build fails: file locked by `Quotes.Api` | a previous `dotnet run` is still running; stop the `Quotes.Api` process |
| `Connection string 'QuotesDb' is not configured` | run with the `http` launch profile (Development), or set `ConnectionStrings__QuotesDb` |
| `/health/ready` unhealthy | migrations not applied: `docker compose up db-migrations` or `scripts/migrate.ps1` |
| Port 5432 or 8080 in use | change `POSTGRES_PORT` / `API_PORT` in `.env` |
| `dotnet format` reports line endings | run `dotnet format AdvisorQuotes.slnx` (`.gitattributes` keeps CRLF consistent once in git) |
| Diagrams not regenerating | install the Mermaid CLI, or run the script with `-Force` |
