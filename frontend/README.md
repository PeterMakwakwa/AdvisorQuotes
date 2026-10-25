# Advisor Quotes UI

A React UI for the Advisor Quotes API. An adviser captures a quote, browses and filters quotes, opens one, and
moves it through its statuses.

React 19 · TypeScript · Vite · shadcn/ui (Radix + Tailwind CSS v4) · TanStack Query · React Router ·
React Hook Form + Zod · Vitest + Testing Library

## Independent of the backend

The UI is a separate application that only talks to the API over HTTP. It shares no code or build with the .NET
solution:

- **Its only dependency is the API contract**, `/api/v1/quotes`, described in [docs/api.md](../backend/docs/api.md) and the
  API's OpenAPI document (`/openapi/v1.json`). `src/api/` is the only code that knows the API exists.
- **Separate build.** It has its own `package.json`, its own `Dockerfile` (nginx), its own CI job, and its own compose
  service (`web`). The .NET solution doesn't reference it.
- **The API address is not built in.** The browser calls `/api` on the UI's own origin, and a proxy forwards those
  calls to the API. In development the proxy is the Vite dev server; in the container it is nginx. The proxy target
  is configuration (`QUOTES_API_URL`), so one build runs against any environment and the API needs no CORS policy.

```
browser ──► UI origin ──┬── /          static files (the React app)
                        └── /api/*  ──► proxy ──► QUOTES_API_URL (the Quotes API)
```

This folder can move to its own repository, or be deployed on its own (for example to S3 + CloudFront with `/api/*`
routed to the API), without code changes.

## Run it

**The API must be running first.** The UI has no data of its own: every page loads from the API. If the API isn't
reachable, pages show *"Could not reach the Quotes API. Check that it is running."*

Commands below run from the repository root unless they start with `cd frontend`.

### Prerequisites

| Tool | Needed for | Check |
| --- | --- | --- |
| Docker Desktop (running) | The API and PostgreSQL in every option, and the UI in option 1 | `docker version` |
| Node.js 24 (24.14 is what CI and the Dockerfile use) with npm | Options 2 and 3, and the tests | `node -v` |
| .NET 10 SDK | Option 3 only | `dotnet --version` |

The first time, create the compose settings file: `cp .env.example .env` (PowerShell: `Copy-Item .env.example .env`).

### Option 1: everything in Docker (no Node.js needed)

The quickest way to see the whole system.

```bash
docker compose up --build
```

Compose starts PostgreSQL, applies the migrations, then starts the API and then the UI.

| | URL |
| --- | --- |
| UI | http://localhost:3000 |
| API | http://localhost:8080/api/v1/quotes |
| API reference | http://localhost:8080/scalar |

Use `docker compose down` to stop. Add `-v` to also delete the database. To use a different port, set `WEB_PORT` in
`.env`.

### Option 2: UI dev server with hot reload, API in Docker (for UI work)

**1. Start the API** (compose starts PostgreSQL and the migrations first):

```bash
docker compose up --build -d api
```

**2. Check the API is up.** It should report `"status":"Healthy"`:

```bash
curl http://localhost:8080/health/ready
```

**3. Start the UI:**

```bash
cd frontend
npm ci          # first time, or after package-lock.json changes
npm run dev     # http://localhost:5173
```

The dev server forwards `/api` to `http://localhost:8080` by default, so this needs no configuration. Edits reload in
the browser. Stop with `Ctrl+C`, and stop the API with `docker compose down`.

### Option 3: UI dev server, API from source (for full-stack debugging)

**1. Start PostgreSQL and apply the migrations:**

```bash
docker compose up -d postgres db-migrations
```

**2. Run the API** (it listens on http://localhost:5039):

```bash
dotnet run --project backend/src/Quotes.Api --launch-profile http
```

**3. Start the UI.** In a second terminal:

```bash
cd frontend
npm ci
npm run dev:source              # http://localhost:5173
```

`dev:source` loads the committed `.env.source`, which forwards `/api` to `http://localhost:5039`, so this needs no
configuration. `npm run dev` still forwards to the Docker API.

### Check it works

1. Open the UI. The **Quotes** page lists the quotes in the database. A new database has none yet, so the page shows
   "No quotes found".
2. The **Adviser ID** box in the header (default `ADV-001`) stands in for sign-in. New quotes are captured under that
   id, and **My quotes** filters the list to it.
3. Choose **New quote**, fill in the form and choose **Capture quote**. The quote opens with its premium priced by the
   API.
4. Use **Send to client**, then **Mark accepted** or **Decline**, to move it through its statuses. Each change appears under
   **History**.

### Configuration

| Variable | Where | Default | Purpose |
| --- | --- | --- | --- |
| `QUOTES_API_URL` | `frontend/.env.local` (dev server, preview), `frontend/.env.source` (`npm run dev:source`) or the container's environment | `http://localhost:8080` (dev), `http://localhost:5039` (dev:source), `http://api:8080` (container) | Where `/api` is forwarded |
| `VITE_TRACE_VIEWER_URL` | `frontend/.env.local`, read at build time | not set | Makes trace ids link to a trace viewer, for example `http://localhost:16686` for Jaeger |
| `WEB_PORT` | root `.env` | `3000` | Host port for the compose `web` service |

### Scripts

Run these in `frontend/`.

| Script | What it does |
| --- | --- |
| `npm run dev` | Dev server with hot reload on port 5173, forwarding `/api` to the Docker API (8080) |
| `npm run dev:source` | The same, forwarding `/api` to the API run from source (5039) |
| `npm test` | Unit, component and accessibility tests (Vitest, jsdom). They don't need the API |
| `npm run lint` / `npm run typecheck` | ESLint / TypeScript project build |
| `npm run build` | Production build into `dist/` |
| `npm run preview` | Serves `dist/` on port 4173, with the same `/api` forwarding |

**Container on its own:** `docker build -t advisor-quotes-web frontend` builds the nginx image.
`docker run -p 3000:80 -e QUOTES_API_URL=http://host.docker.internal:8080 advisor-quotes-web` runs it against an API on
the host.

### Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| *"Could not reach the Quotes API. Check that it is running."* | The API isn't running or isn't at `QUOTES_API_URL`. Run `docker compose ps`, then `curl http://localhost:8080/health/ready`. For option 3, check you started the UI with `npm run dev:source` and the API on port 5039 |
| The API container keeps restarting | It can't reach PostgreSQL or the migrations failed. Check `docker compose logs db-migrations api` |
| The dev server opens on 5174, not 5173 | Port 5173 is in use, so Vite picked the next free port. Use the URL it prints |
| Port 3000 or 8080 is already in use | Set `WEB_PORT` or `API_PORT` in `.env`, then run `docker compose up` again |
| `npm run dev` or `npm test` fails to resolve modules, under a folder whose name contains `#` (for example `C:\Projects\C#\...`) | Vite treats `#` as the start of a URL fragment. The production build still works. Map a drive letter and run from it: `subst Q: "C:\Projects\C#\Interviews\AdvisorQuotes"`, then `cd Q:\frontend`. `subst Q: /d` removes it |
| The dev server breaks after you edit `vite.config.ts` (in a `#` folder) | Vite's automatic restart re-resolves the real path. Stop `npm run dev` and start it again |

## Structure

```
src/
  api/                 HTTP client, contracts and the quotes endpoints; no React here
  app/                 Layout, providers, lazy routes, QueryClient defaults
  components/ui/       shadcn/ui components (generated by the shadcn CLI; ours to edit)
  components/          App-wide pieces built on them: ErrorAlert, TraceId, PageHeader, ThemeToggle
  features/
    adviser/           Who the adviser is (auth is out of scope)
    quotes/
      pages/           Route components: list, new, detail
      components/      Quote-specific UI
      queries.ts       TanStack Query hooks and cache keys
      quoteForm.ts     Zod schemas, request mapping, server-error mapping (pure, unit tested)
```

Code is grouped by feature, and dependencies point one way: `pages → components → queries → api`. The `api` layer
can be reused or swapped for a generated client without touching components.

## Design decisions

- **Vite SPA, not Next.js.** The .NET API already owns business logic and data, and this is an adviser tool behind a
  login, so server rendering and SEO add nothing. The build is static files, which suit S3 + CloudFront with `/api/*`
  routed to the API, with no Node runtime to host. Next.js would earn its place for a public, SEO-sensitive page or a
  backend-for-frontend.
- **shadcn/ui.** Accessible Radix primitives styled with Tailwind, copied into `components/ui` so we own them (no UI
  library version lock-in). The theme is CSS variables (`index.css`): a brand blue plus `success`/`info` tokens for
  status badges, with light, dark and system modes via `next-themes`. Native `<select>` and date inputs are used where
  they beat custom widgets (mobile pickers, typing a birth year).
- **Forms use React Hook Form + Zod with shadcn `Field`**, the pattern shadcn documents. Each Zod schema is the single
  source of client rules and is unit tested on its own. Server errors go through `setError`, so they appear in the same
  place as client errors.
- **Status changes are confirmed in a dialog.** It explains the change, asks for a reason (required when declining),
  and confirms with a toast that carries the trace id.
- **Code splitting.** Pages are lazy routes, and framework code (React, router, query, UI primitives) sits in its own
  chunks, so an app deploy doesn't invalidate the browser's cached framework chunks.
- **Server state lives in TanStack Query**, not in component state or a global store. It handles caching, request
  cancellation, retries and invalidation. A successful mutation writes the returned quote straight into the detail
  cache and invalidates the lists. Queries don't retry 4xx errors (they won't fix themselves). Network errors and 5xx
  get two more tries.
- **Duplicate submissions.** The form derives an `Idempotency-Key` from the payload. The key stays the same while the
  payload is unchanged and changes when the payload does. A double-click, or a retry after a lost response, therefore
  reuses the key, and the API replays the original quote (`Idempotent-Replayed: true`). The UI then says "Already
  captured" rather than creating a duplicate. Editing the form gives a new key, so the API never sees one key with two
  bodies. Network failures on create are retried automatically, which is safe only because of this key. The submit
  button is also disabled while a request is in flight.
- **Same client, warned not blocked.** The idempotency key stops one submission creating two quotes, but quoting a
  client again on purpose is normal (a requote, another option). So before the first submit, the form asks the API for
  the adviser's open quotes (`Draft` or `SentToClient`) for the same client name, date of birth and product
  (`openOnly=true`). If there are any, it lists them with links and the button becomes **Capture anyway**; the next
  submit captures. Changing the client or product checks again. If the check fails, the quote is captured as normal:
  the warning helps, but never blocks.
- **End-to-end trace.** Every request starts a W3C `traceparent` in the browser. The API continues that trace and
  echoes the id in `X-Trace-Id`. After each create or status change the UI shows the trace id, and error banners show
  it too. One id therefore follows a click through the API, PostgreSQL and the outbox in Jaeger or the console exporter.
- **The server is the authority.** The form runs quick checks (required fields, formats, dates in the past, lengths)
  for instant feedback. Product limits stay on the server. Its validation errors (keyed by JSON path) and pricing rule
  codes (for example `Pricing.CoverOutOfRange`) are mapped back onto the matching field. The status actions on offer
  come from `allowedNextStatuses` in the response, so the transition rules exist only in the domain.
- **Conflicts.** A 409 on a status change (someone else moved the quote first) shows the message and reloads the
  quote, so the adviser sees its current status and options.
- **Filters live in the URL**, so a filtered list can be bookmarked or shared and Back restores it. Paging uses the
  API's keyset cursor with "Load more".
- **Accessibility.** The Radix primitives handle focus trapping, keyboard support and ARIA for dialogs, menus and
  tooltips. Labels are tied to inputs, descriptions and errors are linked with `aria-describedby`, `aria-invalid` is
  set, focus moves to the first invalid field, alerts use `role="alert"`/`status`, and there is a skip link. Loading
  states use skeletons, and the layout works at phone width.

## Production readiness

The UI is for Sanlam Connect's advisers, who are external users, so it's built to production standards:

- **Security headers** (`security-headers.conf`, served by nginx):
  - a strict Content Security Policy (`script-src 'self'`: the build has no inline scripts and no `eval`)
  - `frame-ancestors 'none'` against clickjacking
  - `nosniff`, a referrer policy, a permissions policy, and COOP
  - HSTS is left to wherever TLS terminates (CloudFront or the load balancer)
- **Graceful failure.** A route-level error boundary keeps the layout, offers Reload, and shows a support reference
  instead of a stack trace. It also notices a stale lazy chunk after a deploy and tells the adviser to reload.
- **Support references.** Advisers see every request's trace id as a "Support ref". Quoted to support, it leads
  straight to the request's trace and logs.
- **Accessibility is tested.** `src/test/a11y.test.tsx` runs axe-core's WCAG 2.2 A/AA rules over the list, the form
  (with errors showing), the detail page and the status dialog. A Chromium audit of the production build (axe-core, all
  WCAG 2.2 A/AA rules including colour contrast and target size) passed on every page state at 1366, 390 and 320 px,
  in light and dark mode, with no horizontal scrolling, a visible focus indicator on every tab stop, and a clean console.
- **Not indexed.** `robots: noindex`, since it's a signed-in tool. There's a `<noscript>` message, the theme colour
  follows light and dark mode, and `lang="en-ZA"` is set for screen readers.

## What I would add next

- Load the product list (and its cover and term limits) from an API endpoint instead of `products.ts`
- An API-generated TypeScript client (from the OpenAPI document) in place of the hand-written `api/types.ts`
- Real sign-in through a .NET backend-for-frontend (OIDC with an HTTP-only session cookie, so no tokens in the
  browser), taking the adviser id from the identity instead of the header field
- Error monitoring (for example Sentry or CloudWatch RUM) wired into the error boundary, plus product analytics with
  consent
- Playwright end-to-end tests against the composed stack, with axe for colour contrast and cross-browser checks, and
  Lighthouse budgets in CI
- Browser OpenTelemetry (fetch instrumentation) if front-end timings need to appear as spans in the same trace
