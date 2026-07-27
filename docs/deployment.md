# Deployment Guide — LogisticsFlow AI Suite

## Current Public Deployment

- API base URL: https://logisticsflow-api.onrender.com
- Scalar API explorer: https://logisticsflow-api.onrender.com/scalar/v1
- Frontend: https://logistics-flow-faq.vercel.app

All four endpoints are live and database-backed:
- `POST /api/faq/ask`
- `POST /api/quotation/token` and quotation endpoints
- `POST /api/tracking/status`
- `POST /api/tracking/risk-assessment`

## Architecture

- Backend: .NET 9 Web API deployed to Render (free tier)
- Database: Neon (serverless PostgreSQL), free tier
- API explorer: Scalar, served unconditionally by the backend
- Knowledge base: bundled JSON file deployed with the API
- Frontend: React + Vite, deployed separately to Vercel

## Database: Neon

The project uses [Neon](https://neon.tech) for PostgreSQL, chosen over
a traditional always-on host for this stage of the project: generous
free-tier storage for demo-scale data, and scale-to-zero compute that
matches Render's own free-tier "sleeps when idle" behavior — consistent
cost/latency story across the whole stack, with no risk of the database
layer being the one component gating access to Quotation, Tracking, or
Risk Assessment in the public demo.

### Two connection strings

Neon provides both a **direct** connection (straight to Postgres) and a
**pooled** connection (routed through PgBouncer). Which one to use:

- **Direct** — migrations only (`dotnet ef database update`). Some DDL
  operations behave unpredictably through a transaction-mode pooler.
- **Pooled** — the running application's `ConnectionStrings__LogisticsFlowDb`
  in Production. This is what Render is configured to use day-to-day.

### Connection string format gotcha

Neon's dashboard displays connection strings in URI format
(`postgresql://user:pass@host/db?sslmode=require`). This format works
fine with `psql`, but **Npgsql's `NpgsqlConnectionStringBuilder` does
not reliably parse it** — it expects the classic keyword format instead.

Convert before use: Host=<neon-host>;Port=5432;Database=neondb;Username=neondb_owner;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
Also strip any `&channel_binding=require` parameter Neon appends by
default — Npgsql does not recognize this keyword and will throw a
`KeyNotFoundException` on connection-string parsing if it's present.
`SSL Mode=Require` alone still provides TLS encryption; only the extra
channel-binding check is lost.

## Startup Migrations

`Program.cs` runs `db.Database.Migrate()` automatically when
`ASPNETCORE_ENVIRONMENT=Production`. This applies any pending EF
migrations on every Production startup — a no-op if nothing is pending,
so Render's free-tier cold-starts are not meaningfully slowed by this
check. Development migrations are still run manually via `dotnet ef`.

## Required Environment Variables — Backend

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` on deployed environments |
| `ActiveProvider` | Provider selection — `Claude`, `Gemini`, or `Ollama` |
| `Providers__Claude__ApiKey` | Claude API authentication key |
| `Providers__Claude__BaseUrl` | Claude API endpoint URL |
| `Providers__Claude__Model` | Model identifier — verify this matches the intended model before each deploy; a stale value here silently calls the wrong model with no error |
| `Providers__Claude__AnthropicVersion` | Anthropic API version header |
| `Providers__Claude__MaxTokens` | Max tokens per AI call |
| `Providers__Gemini__ApiKey` | Gemini API authentication key |
| `Providers__Gemini__BaseUrl` | Gemini API endpoint URL |
| `Providers__Gemini__Model` | Model identifier — verify against Google's current stable model ID before each deploy |
| `Providers__Gemini__MaxTokens` | Max tokens per AI call |
| `Jwt__Issuer` | JWT issuer identifier |
| `Jwt__Audience` | JWT audience identifier |
| `Jwt__AccessTokenExpiryMinutes` | Token lifetime in minutes |
| `Jwt__SigningKey` | JWT signing secret |
| `AllowedOrigins__0` | Frontend origin permitted for CORS |
| `ConnectionStrings__LogisticsFlowDb` | Neon **pooled** connection string, in keyword format (see above) |

These values are configured in Render's dashboard and must never be
committed to `appsettings.json`.

**Generate a production JWT signing key:**

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

## Required Environment Variables — Frontend

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Backend URL — `https://logisticsflow-api.onrender.com` |

## Smoke Tests

```powershell
Invoke-RestMethod `
  -Uri "https://logisticsflow-api.onrender.com/api/faq/ask" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"query":"What is the difference between LTL and FTL shipping?"}'
```

```powershell
Invoke-RestMethod `
  -Uri "https://logisticsflow-api.onrender.com/api/quotation/token" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"accountId":"ACC-DEMO-001","secret":"<demo-secret>"}'
```

Use the returned token as a Bearer header to test the remaining
account-scoped endpoints:

```powershell
Invoke-RestMethod `
  -Uri "https://logisticsflow-api.onrender.com/api/quotation/quote" `
  -Method POST `
  -Headers @{ Authorization = "Bearer <token>" } `
  -ContentType "application/json" `
  -Body '{"customerQuery":"Any handling notes I should know about?"}'
```

`/api/tracking/status` and `/api/tracking/risk-assessment` follow the
same Bearer-header pattern against seeded demo data (`TRK-DEMO-001`,
`DEMO-LANE-001` through `DEMO-LANE-005`).

## Render Free-Tier Tradeoff

The service runs on Render's free tier with no keep-alive. After
inactivity, the first request spins up the container; subsequent
requests are fast. This is a deliberate demo-hosting tradeoff.

## Future Production Path

Both Render and Neon have paid tiers that remove the sleep/scale-to-zero
behavior if this project ever needs always-on availability or higher
compute. No architectural change is required to upgrade either — both
are configuration/billing changes only.