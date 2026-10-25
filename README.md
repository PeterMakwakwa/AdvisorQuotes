# Advisor Quotes

Advisers create, price, view and progress insurance quotes. The repository holds two separate applications that talk only over HTTP:

| Part | What it is | Docs |
|---|---|---|
| **[backend/](backend/README.md)** | The quotes API: .NET 10, ASP.NET Core and PostgreSQL 17, with clean architecture, CQRS, a transactional outbox, and OpenTelemetry traces, metrics and logs | [backend/README.md](backend/README.md) |
| **[frontend/](frontend/README.md)** | The adviser UI: React 19 + TypeScript, Vite, shadcn/ui, TanStack Query | [frontend/README.md](frontend/README.md) |

**[SOLUTION.md](SOLUTION.md)** covers what the brief asks for: decisions, the premium calculation, duplicate handling, schema management, the end-to-end runtime signal, the AWS plan, how AI tools were used and validated, and where an LLM could fit into the quote flow.

## Quick start: everything at once

You need Docker (Docker Desktop or Rancher Desktop) running. From the repository root:

```bash
cp .env.example .env          # once (PowerShell: Copy-Item .env.example .env)
docker compose up --build
```

Compose starts everything in the right order: **PostgreSQL → migrations (grate) → API → UI**.

| | URL |
|---|---|
| Adviser UI | http://localhost:3000 |
| API | http://localhost:8080/api/v1/quotes |
| API reference (Scalar) | http://localhost:8080/scalar |
| Health | http://localhost:8080/health |
| Metrics | http://localhost:8080/metrics |
| Traces (Jaeger, optional) | http://localhost:16686 |

To turn on Jaeger, uncomment the three tracing lines in `.env` (`OTEL_EXPORTER_OTLP_ENDPOINT`, `COMPOSE_PROFILES`, `TELEMETRY_CONSOLE_EXPORTER`). Then search for the `X-Trace-Id` of any response.

Stop with `docker compose down`, and add `-v` to also delete the database.

## Running the parts separately: which one first

**The backend always comes first.** The UI has no data of its own, so every page loads from the API.

1. **Start the backend.** The simplest way is `docker compose up --build api` from the root. To run from source in Visual Studio or with `dotnet run`, follow [backend/README.md](backend/README.md#run-it-locally).
2. **Check it's up:** `curl http://localhost:8080/health/ready` should report `Healthy` (use port `5039` for the API run from source).
3. **Start the UI** with hot reload, `cd frontend && npm ci`, then:
   - `npm run dev` when the API runs in Docker (port 8080), or
   - `npm run dev:source` when the API runs from source (port 5039).

   Open http://localhost:5173. Neither needs configuration. The options are in [frontend/README.md](frontend/README.md#run-it).

## Tests

| Part | Command | Tests |
|---|---|---|
| Backend | `cd backend && dotnet test` | 156: unit, architecture, and integration against real PostgreSQL (needs Docker) |
| Frontend | `cd frontend && npm ci && npm test` | UI tests, including axe accessibility checks |

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs both suites on every push and pull request:
- **Backend:** formatting, a warnings-as-errors Release build, the tests with coverage, and a vulnerable-package audit.
- **Frontend:** lint, typecheck, tests, build and `npm audit`.
- **Docker:** both images are built and the compose file is validated.

## Repository layout

```
AdvisorQuotes/
├── backend/               .NET solution, schema scripts, backend tests and docs   → backend/README.md
├── frontend/              React adviser UI                                        → frontend/README.md
├── docker-compose.yml     the whole system: PostgreSQL, migrations, API, UI, Jaeger
├── .env.example           compose settings (copy to .env)
├── .github/workflows/     CI for both parts
├── SOLUTION.md            decisions, signal walkthrough, AWS plan
└── README.md              this page
```
