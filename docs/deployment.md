# Deployment Guide — LogisticsFlow FAQ Assistant

## Current Public Deployment

The public API demo is deployed on Render:

- API base URL: https://logisticsflow-api.onrender.com
- Scalar API explorer: https://logisticsflow-api.onrender.com/scalar/v1
- Verified endpoint: `POST /api/faq/ask`

This deployment is intentionally scoped to the FAQ workflow and requires
no persistent database.

## Architecture

- Backend: .NET 9 Web API deployed to Render (free tier)
- API explorer: Scalar, served unconditionally by the backend
- Knowledge base: bundled JSON file deployed with the API
- Frontend: React + Vite, deployable separately to Vercel
- Database-backed modules: not enabled in the current public demo

## Render Free-Tier Tradeoff

The service runs on Render's free tier with no keep-alive configuration.
After inactivity, the first request spins up the container. Subsequent
requests are fast.

This is a deliberate demo-hosting tradeoff. The application is designed
for zero-friction migration to Railway or Azure when persistent database
infrastructure and stronger SLAs are required.

## Current Demo Scope

Supported in the current public deployment:

- `POST /api/faq/ask`
- Scalar API documentation at `/scalar/v1`
- Grounded FAQ responses with source citation
- Confidence scoring
- Escalation for low-confidence or ungrounded answers

Not part of the current public demo:

- Quotation endpoints (appear in Scalar but require a database)
- Order Tracking (Phase 3, planned)
- Booking (Phase 4, planned)

## Required Environment Variables — Backend

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` on deployed environments |
| `ActiveProvider` | Provider selection — `Claude` or `Ollama` |
| `Providers__Claude__ApiKey` | Claude API authentication key |
| `Providers__Claude__BaseUrl` | Claude API endpoint URL |
| `Providers__Claude__Model` | Model identifier, e.g. `claude-sonnet-4-6` |
| `Providers__Claude__AnthropicVersion` | Anthropic API version header |
| `Providers__Claude__MaxTokens` | Max tokens per AI call |
| `Jwt__Issuer` | JWT issuer identifier |
| `Jwt__Audience` | JWT audience identifier |
| `Jwt__AccessTokenExpiryMinutes` | Token lifetime in minutes |
| `Jwt__SigningKey` | JWT signing secret — generate with PowerShell command below |
| `AllowedOrigins__0` | Frontend origin permitted for CORS |

These values are configured in the hosting platform dashboard and must
never be committed to `appsettings.json`.

**Generate a production JWT signing key:**

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

## Database Configuration

The current Render deployment does not require a database for the FAQ
workflow. Startup migrations are intentionally disabled — the migration
block in `Program.cs` is guarded and inactive for the current demo path.

When enabling Quotation in a production environment, add:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__LogisticsFlowDb` | Npgsql-format PostgreSQL connection string |

A production Quotation deployment requires a live PostgreSQL instance and
a deliberate migration strategy.

## Required Environment Variables — Frontend

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Backend URL, e.g. `https://logisticsflow-api.onrender.com` |

## Smoke Test

```powershell
Invoke-RestMethod `
  -Uri "https://logisticsflow-api.onrender.com/api/faq/ask" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"query":"What is the difference between LTL and FTL shipping?"}'
```

Expected response fields: `answer`, `category`, `confidenceScore`,
`escalationBoolean`, `groundingSources`, `sessionId`.

## Escalation Test

```powershell
Invoke-RestMethod `
  -Uri "https://logisticsflow-api.onrender.com/api/faq/ask" `
  -Method POST `
  -ContentType "application/json" `
  -TimeoutSec 30 `
  -Body '{"query":"Can you book my shipment from Lagos to London tomorrow?"}'
```

Expected: `confidenceScore: 0`, empty `groundingSources`,
`escalationBoolean: true`.

## Future Production Path

The application is structured to migrate to Railway, Azure, or another
host with persistent PostgreSQL, enabled Quotation endpoints, a managed
migration workflow, and stronger uptime SLAs when required.