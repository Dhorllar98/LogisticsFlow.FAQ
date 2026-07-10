# LogisticsFlow AI Suite

A production-grade AI system for multimodal logistics operations across
Land, Sea, and Air freight — spanning grounded customer support, quoting,
and order tracking.

Built on Clean Architecture with explicit data governance,
confidence-aware escalation, AI response validation, resilience patterns,
and a provider-agnostic model interface.

## Live Demo

- API base URL: https://logisticsflow-api.onrender.com
- API explorer: https://logisticsflow-api.onrender.com/scalar/v1
- Frontend: https://logistics-flow-faq.vercel.app
- Verified endpoint: `POST /api/faq/ask`

This public demo runs on Render's free tier with no keep-alive. The first
request after inactivity spins up the container — subsequent requests are
fast. This is a deliberate infrastructure tradeoff for the public demo.
Quotation and Order Tracking are fully built and tested but require
persistent database infrastructure not yet provisioned in production —
see [`docs/deployment.md`](docs/deployment.md) for the migration path to
Railway once funded.

## Verified Smoke Test

```json
POST /api/faq/ask
{ "query": "What is the difference between LTL and FTL shipping?" }
```

Expected response includes `answer`, `confidenceScore`, `groundingSources`,
and `escalationBoolean: false` for grounded answers.

Out-of-scope requests intentionally trigger escalation:

```json
POST /api/faq/ask
{ "query": "Can you book my shipment from Lagos to London tomorrow?" }
```

Expected: `confidenceScore: 0`, empty `groundingSources`,
`escalationBoolean: true`.

## The Problem

Logistics support teams handle high-volume, repetitive inquiries about
transit times, customs procedures, freight terminology, pricing rules,
and shipment status — routine work that slows response times and pulls
attention away from complex client issues.

## The Solution

Three integrated modules, each scoped deliberately rather than uniformly
over-engineered:

- **FAQ** — a curated 33-entry knowledge base covering Land, Sea, Air,
  and general logistics operations. Every response stays inside the
  knowledge boundary or escalates — it does not guess.
- **Quotation** — account-scoped rate lookups composed into a
  customer-facing message, with a full Tier 2 redact/restore lifecycle
  around every cloud AI call.
- **Order Tracking** — account-scoped shipment status lookups composed
  into a plain-English summary, using the same redaction discipline as
  Quotation.

All three are single-call, non-agentic by explicit architectural
decision — documented in `CLAUDE.md` — with Semantic Kernel reserved for
a future phase where chained AI reasoning has a genuine business
justification, not adopted for architectural novelty.

## Project Status

| Phase | Status | Notes |
|---|---|---|
| Phase 1: FAQ | Live on Render | Public FAQ endpoint deployed and smoke-tested |
| Phase 2: Quotation | Built, not in public demo | Requires persistent database infrastructure |
| Phase 2.5: Multi-provider abstraction | Completed | Provider-agnostic `ILlmClient`; typed `ClaudeSettings` / `OllamaSettings` |
| Phase 3: Order Tracking | Completed, not in public demo | Single-call, non-agentic (Option A); same DB dependency as Quotation |
| Sonnet 5 migration | Completed | Model, token budget, and refusal-path handling verified across all three modules |
| Phase 3 tech debt closure | Completed | `ShipmentMode` converted from string to enum; exception→status-code mapping verified correct |
| Phase 3.5: Delay/risk assessment | Planned | Aggregate lane-history comparison (no cross-account data exposure); reviewed for Semantic Kernel adoption and found non-agentic — all data is deterministically fetched, no dynamic tool selection required, so it stays single-call per the Phase-Scoped AI Orchestration Exception |
| Phase 4: Booking | Planned | Agentic workflow phase |

The current public deployment intentionally exposes the FAQ workflow
only. Quotation and Tracking endpoints appear in Scalar but require a
database and are not part of the current public demo.

## Architecture

Built on Clean Architecture:
Domain → Application → Infrastructure → Presentation

The FAQ workflow uses a RAG-lite pattern: the curated knowledge base is
injected directly into the model context rather than using a vector
database — appropriate at the current 33-entry scale.

Full architecture reasoning is in [`docs/architecture.md`](docs/architecture.md).

## Key Features

- Grounded FAQ responses with cited knowledge-base source IDs
- Confidence-aware escalation (threshold: 0.70), forced regardless of
  score when grounding sources are empty
- Account-scoped Quotation and Tracking access — enforced at the query
  level via JWT claims, never via client-supplied identifiers
- Explicit Tier 1 / Tier 2 / Tier 3 data classification with a
  request-lifetime-only redaction lifecycle around every cloud AI call
- FluentValidation on every request and response, with a consistent
  single-error-per-field contract across all endpoints
- Consistent JSON error contract across every failure mode, including
  authentication failures
- Rate limiting on all public endpoints, independently tuned per
  module, with verified `Retry-After` headers on 429 responses
- Retry, timeout, and circuit-breaker patterns around every AI call
- Global exception handling with deliberate status-code mapping
- Provider-agnostic AI client abstraction (`ILlmClient`), currently
  backed by Claude Sonnet 5

## Security Considerations

- Explicit Tier 1/2/3 data classification enforced before any AI call
  — see [`docs/data-classification.md`](docs/data-classification.md)
- User input is treated as untrusted regardless of data sensitivity,
  and structurally separated from the system prompt at the API boundary
- Self-reported model confidence is never trusted as a security boundary
- Account identity for Quotation and Tracking is derived exclusively
  from validated JWT claims — never from client-supplied request
  fields, closing a cross-account data exposure risk found and fixed
  during development
- Cross-account data access is architecturally excluded, not just
  policy-restricted, wherever the design permits it — e.g. Phase 3.5's
  planned lane-history comparison is deliberately scoped to aggregate,
  depersonalized statistics rather than shipment-to-shipment comparison,
  removing the tenant-boundary risk by design rather than by discipline
- API keys and secrets live in environment variables only, never in
  committed config
- HTTP security headers (X-Content-Type-Options, X-Frame-Options,
  Referrer-Policy, Permissions-Policy) are a known, logged gap — not
  yet implemented; tracked in
  [`docs/security-hardening-checklist.md`](docs/security-hardening-checklist.md)

See [`docs/security-hardening-checklist.md`](docs/security-hardening-checklist.md)
and [`docs/deployment.md`](docs/deployment.md).

## Tech Stack

- **Backend**: .NET 9, ASP.NET Core Web API
- **Database**: PostgreSQL via Npgsql/EF Core
- **Validation**: FluentValidation
- **Resilience**: Polly
- **Logging**: Serilog
- **API docs**: Scalar / OpenAPI
- **Auth**: JWT Bearer
- **AI provider**: Claude Sonnet 5, behind a provider-agnostic `ILlmClient`
  interface (local-model routing via Ollama scoped for future Tier 3 work)
- **Frontend**: React 18, TypeScript, Tailwind CSS v4, Vite
- **Testing**: xUnit + Moq, unit/repository/integration layers, 64 tests passing