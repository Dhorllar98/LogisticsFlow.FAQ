# LogisticsFlow AI: Knowledge-Grounded FAQ Assistant

A production-grade, RAG-lite AI assistant for tier-1 customer support in
multimodal logistics operations across Land, Sea, and Air freight.

Built on Clean Architecture with explicit data governance,
confidence-aware escalation, AI response validation, resilience patterns,
and a provider-agnostic model interface introduced in Phase 2.5.

## Live Demo

- API base URL: https://logisticsflow-api.onrender.com
- API explorer: https://logisticsflow-api.onrender.com/scalar/v1
- Frontend: https://logistics-flow-faq.vercel.app
- Verified endpoint: `POST /api/faq/ask`

This public demo runs on Render's free tier with no keep-alive. The first
request after inactivity spins up the container — subsequent requests are
fast. This is a deliberate infrastructure tradeoff for the public demo.
The application is designed for zero-friction migration to Railway or Azure
when persistent database infrastructure and production SLAs are required.
Full reasoning is in [`docs/deployment.md`](docs/deployment.md).

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
transit times, customs procedures, freight terminology, and pricing rules.
These routine questions slow response times and take attention away from
complex client issues.

## The Solution

This assistant answers customer questions using a curated 33-entry
knowledge base covering Land, Sea, Air, and general logistics operations.
Every response stays inside the knowledge boundary or escalates — it does
not guess.

## Project Status

| Phase | Status | Notes |
|---|---|---|
| Phase 1: FAQ | Live on Render | Public FAQ endpoint deployed and smoke-tested |
| Phase 2: Quotation | Built, not enabled in public demo | Requires persistent database infrastructure |
| Phase 2.5: Multi-provider abstraction | Completed | Provider-agnostic `ILlmClient` added; typed `ClaudeSettings` / `OllamaSettings` replace single shared config |
| Phase 3: Tracking | Planned | Next implementation phase |
| Phase 4: Booking | Planned | Agentic workflow phase |

The current public deployment intentionally exposes the FAQ workflow only.
Quotation endpoints appear in Scalar but require a database and are not
part of the current public demo.

## Architecture

Built on Clean Architecture:
Domain → Application → Infrastructure → Presentation

The FAQ workflow uses a RAG-lite pattern: the curated knowledge base is
injected directly into the model context rather than using a vector
database. That is appropriate for the current 33-entry knowledge set.

Full architecture reasoning is in [`docs/architecture.md`](docs/architecture.md).

## Key Features

- Grounded responses with cited knowledge-base source IDs
- Confidence-aware escalation (threshold: 0.70)
- Forced escalation when grounding sources are empty, regardless of score
- FluentValidation on every request and response
- Rate limiting on public endpoints with per-IP partitioning
- Retry, timeout, and circuit-breaker patterns around every AI call
- Response caching for repeated FAQ-style questions
- Global exception handling
- Provider-agnostic AI client abstraction (`ILlmClient`)

## Security Considerations

- FAQ knowledge base is classified Tier 1 — general public logistics knowledge
- User input is treated as untrusted regardless of data sensitivity
- User input is structurally separated from the system prompt at the API call boundary
- Self-reported model confidence is never trusted as a security boundary
- Empty `groundingSources` forces escalation regardless of confidence score
- API keys and secrets live in environment variables only, never in committed config

See [`docs/data-classification.md`](docs/data-classification.md),
[`docs/security-hardening-checklist.md`](docs/security-hardening-checklist.md),
and [`docs/deployment.md`](docs/deployment.md).

## Tech Stack

- **Backend**: .NET 9, ASP.NET Core Web API
- **Validation**: FluentValidation
- **Resilience**: Polly
- **Logging**: Serilog
- **API docs**: Scalar / OpenAPI
- **AI provider**: Claude API, behind a provider-agnostic `ILlmClient` interface
- **Frontend**: React 18, TypeScript, Tailwind CSS v4, Vite