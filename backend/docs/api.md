# API

Interactive reference: `/scalar` (OpenAPI document at `/openapi/v1.json`). The `.http` file `src/Quotes.Api/Quotes.Api.http` runs the whole flow.

**On this page:** [Endpoints](#endpoints) · [Conventions](#conventions) · [Create a quote](#create-a-quote) · [Change status](#change-status) · [List quotes](#list-quotes) · [Errors](#errors) · [Rate limiting](#rate-limiting) · [Versioning](#versioning)

## Endpoints

| Method | Route | Purpose | Success |
|---|---|---|---|
| POST | `/api/v1/quotes` | Capture and price a quote | `201` + `Location` |
| GET | `/api/v1/quotes` | List quotes, newest first (filters + keyset paging) | `200` |
| GET | `/api/v1/quotes/{id}` | One quote with history and `allowedNextStatuses` | `200` |
| POST | `/api/v1/quotes/{id}/status-changes` | Move the quote to a new status | `200` |
| GET | `/health/live` · `/health/ready` · `/health` | Liveness · readiness (database) · every check incl. the outbox | `200` (`503` when unhealthy) |
| GET | `/metrics` | Prometheus scrape endpoint | `200` |

## Conventions

| Header | Direction | Meaning |
|---|---|---|
| `Idempotency-Key` | request (create) | Optional, up to 100 characters. Makes the create safe to retry. |
| `Idempotent-Replayed: true` | response | This is the original quote for a repeated submission; nothing new was created. |
| `X-Trace-Id` | response | The W3C trace id of the request: find its trace in Jaeger and its log lines (`trace=<id>` in text logs, the `@tr` field in JSON logs). |
| `api-supported-versions` | response | API versions this server supports. |
| `Retry-After` | response (429) | Seconds until the rate limit window resets. |

- JSON with camelCase names; enums as strings (`"SentToClient"`); dates as ISO 8601 (`"1991-01-01"`, timestamps in UTC with offset).
- Money is a decimal with 2 places; the currency comes from the rate table (`ZAR`).
- Authentication is out of scope, so `adviserId` and `changedBy` are supplied by the caller.

## Create a quote

```http
POST /api/v1/quotes
Idempotency-Key: 6f1c2b7e-3a4d-4e5f-9b8c-1d2e3f4a5b6c
Content-Type: application/json

{
  "adviserId": "ADV-001",
  "adviserReference": "CRM-2026-0042",
  "client": { "fullName": "Thandi Nkosi", "dateOfBirth": "1991-01-01", "isSmoker": false },
  "product": { "productCode": "TERM_LIFE", "coverAmount": 1000000, "termYears": 20 },
  "notes": "Client wants cover until retirement"
}
```

```json
{
  "id": "01a16db4-73de-7b10-bfaf-27b17ab422bf",
  "externalReference": "Q-2026-000001",
  "adviserId": "ADV-001",
  "adviserReference": "CRM-2026-0042",
  "client": { "fullName": "Thandi Nkosi", "dateOfBirth": "1991-01-01", "isSmoker": false },
  "product": { "productCode": "TERM_LIFE", "coverAmount": 1000000, "termYears": 20 },
  "premium": { "monthlyAmount": 300.00, "currency": "ZAR", "pricingVersion": "2026.1" },
  "status": "Draft",
  "allowedNextStatuses": ["SentToClient", "Declined"],
  "notes": "Client wants cover until retirement",
  "createdAt": "2026-10-24T11:38:25.118236+00:00",
  "updatedAt": "2026-10-24T11:38:25.118236+00:00",
  "statusHistory": [
    { "fromStatus": null, "toStatus": "Draft", "reason": "Quote created", "changedBy": "ADV-001", "changedAt": "2026-10-24T11:38:25.118236+00:00" }
  ]
}
```

| Field | Rules |
|---|---|
| `adviserId` | required, up to 50 characters |
| `adviserReference` | optional, up to 50; the adviser's own reference (not unique). `externalReference` is always issued by the service. |
| `client.fullName` | required, up to 200 |
| `client.dateOfBirth` | required, in the past; the age on the quote date (the calendar date in `Pricing:TimeZone`, South Africa by default) must fall in a rated age band |
| `client.isSmoker` | required |
| `product.productCode` | required; must be a product in the rate table (`TERM_LIFE`, `SERIOUS_ILLNESS`) |
| `product.coverAmount` | required, > 0, at most 2 decimal places, within the product's cover range |
| `product.termYears` | required, within the product's term range; age + term within the product's maximum age at expiry |
| `notes` | optional, up to 2000 |

**Duplicates:** the same `Idempotency-Key` and body within 24 hours, or the same body without a key within 10 minutes, return the original quote with `Idempotent-Replayed: true`. Spacing, letter-case and number-formatting differences (`1000000` vs `1000000.00`) don't count as a different body. The same key with a different body is `422 Idempotency.KeyReused`. A replay returns the quote as it is **now** (for example after a status change), not a snapshot of the first response.

**References:** `externalReference` is `Q-<year captured>-<number>`. The number comes from one sequence for all years, so it is unique and increasing but does not restart in January, and may have gaps.

## Change status

```http
POST /api/v1/quotes/01a16db4-73de-7b10-bfaf-27b17ab422bf/status-changes
Content-Type: application/json

{ "newStatus": "SentToClient", "reason": "Emailed to client", "changedBy": "ADV-001" }
```

Returns the updated quote. Allowed moves: `Draft → SentToClient`, `Draft → Declined`, `SentToClient → Accepted`, `SentToClient → Declined`. `Accepted` and `Declined` are final; declining needs a `reason` (up to 500 characters). `changedBy` is required (up to 100).

**Retries:** a status change has no idempotency key. If a retry follows a lost response, the quote already has the new status and the retry gets `409 Quote.StatusUnchanged`: treat that code as "already done" and `GET` the quote. `409 Quote.InvalidStatusTransition` is a real conflict.

## List quotes

```http
GET /api/v1/quotes?adviserId=ADV-001&status=SentToClient&pageSize=20
GET /api/v1/quotes?adviserId=ADV-001&pageSize=20&cursor=<nextCursor from the previous page>
```

```json
{
  "items": [ { "id": "…", "externalReference": "Q-2026-000004", "adviserId": "ADV-001", "adviserReference": null,
               "clientName": "Thandi Nkosi", "productCode": "TERM_LIFE", "coverAmount": 700000.00, "monthlyPremium": 210.00,
               "currency": "ZAR", "status": "Draft", "createdAt": "…", "updatedAt": "…" } ],
  "pageSize": 20,
  "totalCount": 4,          // first page only; null on later pages
  "nextCursor": null
}
```

Filters: `adviserId`, `adviserReference` (exact), `status`, `clientName` (exact, ignoring case and surrounding spaces), `clientDateOfBirth` (`yyyy-MM-dd`), `productCode` (ignoring case), and `openOnly=true` (only quotes that can still change status: `Draft` and `SentToClient`).

The UI uses the client filters to warn an adviser, without blocking, before capturing another quote for a client who already has an open quote for the same product:

```http
GET /api/v1/quotes?adviserId=ADV-001&clientName=Thandi%20Nkosi&clientDateOfBirth=1990-05-01&productCode=TERM_LIFE&openOnly=true&pageSize=5
```

Paging: `pageSize` 1–100 (default 20). Paging is keyset-based: pass `nextCursor` back as `cursor`; it is `null` on the last page. Pages stay stable while new quotes arrive, and deep pages cost the same as the first. `totalCount` is only returned with the first page (no `cursor`), so later pages never re-count every match. The cursor is opaque; an invalid one is a field error.

## Errors

Every error is an RFC 9457 problem details response with a stable, machine-readable `code` and the `traceId`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "A quote cannot move from Accepted to Declined.",
  "traceId": "00-c88401c3b5630cf16b7c5d1ace4790e4-7d63b142e7f6383a-01",
  "code": "Quote.InvalidStatusTransition"
}
```

Validation failures add `errors`, keyed by field path (`client.fullName`, `product.coverAmount`, `cursor` …).

| Status | Code | When |
|---|---|---|
| 400 | `Validation.Failed` | Request shape invalid (see `errors`) |
| 400 | `Quote.AdviserRequired`, `Quote.ClientNameRequired`, `Quote.DateOfBirthNotInPast`, `Quote.CoverAmountNotPositive`, `Quote.TermNotPositive`, `Quote.PremiumNegative`, `Quote.ChangedByRequired`, `Quote.DeclineReasonRequired` | Domain invariants |
| 400 | `Pricing.UnknownProduct`, `Pricing.CoverOutOfRange`, `Pricing.TermOutOfRange`, `Pricing.TermNotRated`, `Pricing.AgeNotInsurable`, `Pricing.AgeAtExpiryExceeded` | The product's rules don't allow this quote |
| 400 | `Idempotency.KeyTooLong` | `Idempotency-Key` over 100 characters |
| 404 | `Quote.NotFound` | No quote with that id |
| 409 | `Quote.InvalidStatusTransition` | The move isn't allowed from the current status |
| 409 | `Quote.StatusUnchanged` | The quote already has that status |
| 409 | `Quote.ConcurrentUpdate` | Someone else changed the quote since it was read; reload and retry |
| 422 | `Idempotency.KeyReused` | Same `Idempotency-Key`, different body |
| 429 | `RateLimit.Exceeded` | Too many requests from this client; see `Retry-After` |
| 500 | (none) | Unexpected error; details are logged with the trace id, never returned |

## Rate limiting

A fixed window per client (by IP address, as there is no authentication) on the quote endpoints: 100 requests per minute by default (`RateLimiting__PermitLimit`, `RateLimiting__Window`). Health, metrics and documentation endpoints are not limited.

## Versioning

URL-segment versioning (`/api/v1/...`) with Asp.Versioning. Responses report `api-supported-versions`; an unknown version returns 404. There is one OpenAPI document per version. A breaking change would ship as `v2` alongside `v1`.

**Pre-release exception:** `totalCount` became nullable (first page only) inside `v1`. Clients that rely on it on every page would break, so after release this would be `v2`, or a new field alongside the old one. It changed in place only because `v1` has no external clients yet.
