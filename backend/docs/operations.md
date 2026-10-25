# Operations

Configuring, observing and running the service. Local setup is in [local-development.md](local-development.md).

**On this page:** [Configuration](#configuration) · [Health checks](#health-checks) · [Traces and logs](#traces-and-logs) · [Metrics](#metrics) · [Outbox](#outbox) · [Database and migrations](#database-and-migrations) · [Rate limiting](#rate-limiting) · [Runbook](#runbook)

## Configuration

Standard .NET configuration: `appsettings.json` → `appsettings.{Environment}.json` → **environment variables** (`__` separates sections). Every options class is validated at start-up, so misconfiguration stops the app instead of letting it misbehave.

| Setting | Environment variable | Default |
|---|---|---|
| Connection string | `ConnectionStrings__QuotesDb` | local value in `appsettings.Development.json` only |
| Database retries | `Database__MaxRetryCount`, `Database__MaxRetryDelay` | `3`, `00:00:05` |
| Rate limit | `RateLimiting__PermitLimit`, `RateLimiting__Window` | `100`, `00:01:00` |
| Forwarded headers | `ForwardedHeaders__Enabled`, `__KnownProxies__0`, `__KnownNetworks__0`, `__ForwardLimit` | `false`, none, none, `1` |
| Log format | `Logging__Format` (`text` or `json`) | text in Development, JSON elsewhere |
| Idempotency | `Idempotency__KeyLifetime`, `Idempotency__FingerprintWindow` | `1.00:00:00`, `00:10:00` |
| Rate table | `Pricing__*` | see `appsettings.json` |
| Outbox | `Outbox__Enabled`, `__PollingInterval`, `__BatchSize`, `__MaxAttempts`, `__LeaseDuration`, `__InitialRetryDelay`, `__MaxRetryDelay`, `__BacklogAgeWarning` | `true`, `2 s`, `50`, `20`, `1 min`, `5 s`, `5 min`, `5 min` |
| Maintenance | `Maintenance__Enabled`, `__Interval`, `__ExpiredIdempotencyKeyRetention`, `__ProcessedOutboxRetention`, `__DeleteBatchSize` | `true`, `1 h`, `1 day`, `7 days`, `5000` |
| Traces | `OTEL_EXPORTER_OTLP_ENDPOINT`, `Telemetry__ConsoleExporter` | unset, `true` |
| Metrics | `Telemetry__ConsoleMetricsExporter`, `Telemetry__OtlpMetrics`, `Telemetry__MetricsExportIntervalSeconds` | `false`, `false`, `15` |
| OpenAPI / Scalar outside Development | `OpenApi__Enabled` | `false` (`true` in compose) |

Secrets never live in the image or the repository: locally `.env` (from `.env.example`), in AWS Secrets Manager.

## Health checks

| Endpoint | Checks | Use |
|---|---|---|
| `/health/live` | none | container liveness: the process responds |
| `/health/ready` | PostgreSQL (`SELECT 1 FROM quotes`: reachable **and** migrated) | load balancer target health |
| `/health` | all checks, including **outbox** | dashboards and alarms |

The outbox check reports **Degraded** (still HTTP 200) when messages have run out of attempts, or the oldest pending message has waited longer than `Outbox:BacklogAgeWarning`. It is deliberately **not** part of readiness: a broker outage must raise an alarm, not take instances out of service, because quotes can still be captured and events are published once the broker recovers.

## Traces and logs

- **Traces** (OpenTelemetry): one trace per request, containing the HTTP span, `quote.create` / `quote.status.change` / `pricing.calculate` with business attributes, every SQL statement, and the later `… publish` span of the request's outbox event. Console by default; OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (Jaeger locally, the ADOT collector in AWS).
- **Logs** (Serilog to stdout): readable text in Development (`trace=<id>` on every line), **compact JSON everywhere else** so log stores can index the fields (`@t` time, `@l` level (omitted for Information), `@m` message, `@i` event id hash, `@tr` trace id, `@sp` span id, plus every named property such as `QuoteId`). Every line carries the trace id, including the publisher's line for an event; use `X-Trace-Id` from any response to find them. Log calls are source-generated `[LoggerMessage]` methods with stable event ids:

  | Event ids | Area | Examples |
  |---|---|---|
  | 1000–1999 | Application | 1001 quote created · 1002 duplicate replayed · 1003 status changed · 1004 request rejected |
  | 2000–2999 | Outbox | 2001 published · 2002 publish failed · 2003 gave up · 2004 lease lost · 2006 dispatch failed |
  | 3000–3999 | Maintenance | 3001 completed · 3003 failed |
  | 4000–4999 | HTTP host | 4001 unhandled exception · 4002 rate limit exceeded |
- **Following a quote:** all requests for a quote carry the `quote.id` attribute; search Jaeger by tag `quote.id=<id>`.

## Metrics

| Metric | Type | Meaning |
|---|---|---|
| `quotes.created` | counter (product) | quotes captured |
| `quotes.status_changes` | counter (from, to) | lifecycle movement |
| `quotes.duplicates_replayed` | counter (key source) | duplicates caught |
| `quotes.rejected` | counter (operation, error code, type) | rejected requests by reason |
| `quotes.monthly_premium` | histogram (product, currency) | premiums of new quotes |
| `quotes.outbox.published` / `.failed` | counter (event type) | event delivery |
| `quotes.outbox.lag` | histogram, seconds | event raised → published |
| `quotes.outbox.pending` | gauge | messages waiting |
| `quotes.outbox.dead_lettered` | gauge | messages that gave up; should be 0 |
| `quotes.outbox.oldest_pending_age` | gauge, seconds | how far behind publishing is |

Plus ASP.NET Core (`http.server.request.duration`, rate-limiting decisions), Npgsql and .NET runtime meters. Prometheus names carry the meter as a prefix (`quotes_application_quotes_created`, `quotes_infrastructure_messaging_quotes_outbox_pending`). The backlog gauges show the last reading (after each dispatcher run and each `/health` call), so a scrape never queries the database.

**Suggested alarms:** 5xx rate; p95 of `http.server.request.duration`; `quotes.outbox.dead_lettered > 0`; `quotes.outbox.oldest_pending_age > 300`; a spike in `quotes.rejected` for one error code.

## Outbox

How it works: [architecture.md](architecture.md#domain-events-and-the-outbox). Useful queries:

```sql
-- Backlog at a glance
SELECT count(*) FILTER (WHERE processed_at IS NULL AND attempts < 20)  AS pending,
       count(*) FILTER (WHERE processed_at IS NULL AND attempts >= 20) AS dead_lettered,
       count(*) FILTER (WHERE locked_until > now())                    AS leased_now
FROM outbox_messages;

-- What is failing, and why
SELECT id, event_type, attempts, next_attempt_at, last_error
FROM outbox_messages
WHERE processed_at IS NULL AND attempts > 0
ORDER BY occurred_at;
```

**Re-drive** messages after fixing the cause (for example a broker permission): reset their attempts, and the dispatcher publishes them on its next run.

```sql
UPDATE outbox_messages
SET attempts = 0, next_attempt_at = NULL, last_error = NULL, locked_until = NULL, lock_token = NULL
WHERE processed_at IS NULL AND attempts >= 20;
```

**Retention:** the maintenance job deletes published messages after `ProcessedOutboxRetention` (7 days) and expired idempotency keys a day after expiry, in batches of `DeleteBatchSize`. It never deletes quotes, their history or dead-lettered messages. The audit trail is the status history plus logs; the outbox is only the delivery mechanism. If proof of publication must be kept, archive rows (for example to S3) before deletion. At very high volume, partition `outbox_messages` by month and drop old partitions.

**Lease tuning:** `LeaseDuration` must comfortably exceed the time to publish one batch. If the log shows "The lease … expired before its outcome was recorded", lengthen the lease or reduce `BatchSize`.

## Database and migrations

- The schema is owned by `src/dbQuotes/up/*.sql`, applied by **grate**: the `db-migrations` container in compose, `scripts/migrate.ps1` (grate as a .NET tool), or a one-off ECS task in AWS. Each script runs once and is recorded in the `grate` schema; scripts run in a transaction.
- **Evolve** by adding the next numbered script, never by editing an applied one (grate refuses changed one-time scripts). Keep changes backwards compatible for one release (expand, then contract), so rolling deploys are safe.
- The application never runs migrations and needs no DDL permissions.

## Rate limiting

Fixed window per client IP on the quote endpoints. Rejections are logged (event 4002) and counted by the ASP.NET Core rate-limiting meter. With authentication, the partition would be the adviser instead.

**Behind a load balancer**, every request comes from the load balancer's address, so all clients would share one limit. Enable forwarded headers and trust only the load balancer's network, so the real client IP from `X-Forwarded-For` is used:

```
ForwardedHeaders__Enabled=true
ForwardedHeaders__KnownNetworks__0=10.0.0.0/16     # the VPC range the ALB sits in
```

Only listed proxies and networks (plus loopback) are trusted; trusting `X-Forwarded-For` from anyone would let a client pick its own IP and dodge the limit. Invalid addresses stop start-up. The integration tests prove both cases: disabled, clients behind one proxy share a limit; enabled, each forwarded client has its own.

## Runbook

| Symptom | Check | Action |
|---|---|---|
| `/health/ready` unhealthy | database reachable? migrations applied? | fix connectivity; run grate |
| `/health` outbox Degraded, `dead_lettered > 0` | `last_error` of the failing messages | fix the cause, then re-drive (above) |
| `oldest_pending_age` climbing, no failures | is the dispatcher running? (`Outbox:Enabled`, logs "Outbox dispatch failed") | restart; check database connectivity |
| Warnings "lease … expired before its outcome was recorded" | publish duration vs `LeaseDuration` | lengthen the lease or reduce `BatchSize` |
| Many `429` | which client, `RateLimiting__PermitLimit` | raise the limit or investigate the client |
| `409 Quote.ConcurrentUpdate` reported by users | normal under contention | client reloads and retries |
| App won't start | the log names the invalid setting | fix the configuration |
