# Architecture — LogisticsFlow FAQ Assistant

## Overview

LogisticsFlow is a knowledge-grounded AI assistant for multimodal logistics
operations across Land, Sea, and Air freight.

The current public deployment exposes the Phase 1 FAQ workflow. The broader
project includes database-backed Quotation work and a Phase 2.5
provider-agnostic AI abstraction. The public Render demo runs without a
database dependency.

## Dependency Chain

Domain → Application → Infrastructure → Presentation

Each layer depends only on the layer inward from it. The Domain layer has
no framework dependencies.

## Domain Layer

`LogisticsFlow.Domain` contains:

- `FAQEntry`, conversation entities, logistics category enums
- Business exceptions: `BusinessRuleException`, `KnowledgeBoundaryException`
- Repository and AI client contracts: `IFAQRepository`, `ILlmClient`

Domain defines contracts. It has no knowledge of ASP.NET Core, Claude,
Render, databases, or external infrastructure.

## Application Layer

`LogisticsFlow.Application` contains:

- Request and response DTOs
- FluentValidation validators
- FAQ orchestration service (`FAQService`)
- Prompt construction (`SystemPrompts`)
- Response-shaping and escalation logic
- Service interfaces (`IFAQService`)

The FAQ service loads the knowledge base, sends the request through the
AI provider abstraction, validates the response, and applies escalation
rules.

## Infrastructure Layer

`LogisticsFlow.Infrastructure` contains:

- `ClaudeApiClient` — typed `HttpClient` implementation of `ILlmClient`
- `OllamaApiClient` — stub implementation for future Tier 3 local routing
- `JsonFAQRepository` — loads `data/faq_knowledgebase.json` at startup
- `FAQCacheService` — `IMemoryCache`-backed response cache
- `AppDbContext` — EF Core context for database-backed modules
- `ClaudeSettings`, `OllamaSettings` — typed settings classes bound per provider
- Resilience configuration via `AddStandardResilienceHandler`
- DI registration

Phase 2.5 replaced the single `LlmProviderSettings` class with typed
`ClaudeSettings` and `OllamaSettings`. `ClaudeSettings` carries `ApiKey`,
`BaseUrl`, `Model`, `AnthropicVersion`, and `MaxTokens`. `OllamaSettings`
has no `ApiKey` field — the absence is intentional. Provider-agnosticism
lives at the `ILlmClient` contract layer; the settings layer is
deliberately provider-specific because pretending all providers share an
auth shape is inaccurate.

## Presentation Layer

`LogisticsFlow.API` contains:

- `FAQController` — single endpoint: `POST /api/faq/ask`
- `QuotationController` — database-backed; not part of current public demo
- `TrackingController` — database-backed; account-scoped shipment status
- `RiskAssessmentController` — database-backed; deterministic risk level
  plus AI-composed suggested action, scoped to the authenticated account
- `GlobalExceptionMiddleware`
- `CorsExtensions`, `RateLimitingExtensions`
- JWT Bearer configuration
- Scalar / OpenAPI wiring

Scalar is exposed unconditionally — this is a recruiter-facing demo.

## Middleware Order

GlobalExceptionMiddleware
→ Scalar / OpenAPI
→ HTTPS Redirection
→ CORS
→ Rate Limiting        ← before auth, intentional
→ Authentication
→ Authorization
→ Controllers

Rate limiting is placed before authentication so unauthenticated requests
are throttled at the network boundary rather than reaching the identity
pipeline first.

## RAG-Lite Approach

The FAQ workflow injects the curated 33-entry knowledge base directly into
the model context. A vector database would add retrieval latency and
operational cost without improving retrieval quality at this scale. Vector
search becomes appropriate when the knowledge base grows beyond what
reliably fits in context, or when retrieval needs become more dynamic.

## Input Handling and Prompt-Injection Resistance

The FAQ module accepts one untrusted input: the user's free-text query.

Controls:

- User query is passed as a user-turn message, never interpolated into the system prompt
- System prompt and user query are structurally separated at the API call boundary
- `FAQRequestValidator` enforces a 500-character ceiling and rejects empty queries
- Conversation history is bounded at 6 entries at both the validator and entity level
- Model confidence is not trusted as a security boundary
- Empty `groundingSources` forces escalation regardless of score

This is a non-agentic workflow. There is no tool-calling loop and no
external tool output being reinterpreted as instructions. Those risks
become active in the Booking phase.

## Semantic Kernel Position

Semantic Kernel is intentionally not used for the FAQ phase. The FAQ
workflow is a single-call, non-agentic flow. Per the Phase-Scoped
AI Orchestration Exception declared in `CLAUDE.md`, Semantic Kernel
becomes mandatory when the system introduces multi-step workflows,
tool use, or agentic orchestration — anticipated at the Booking phase.

## Confidence and Escalation Logic

Every FAQ response includes `confidenceScore`, `groundingSources`, and
`escalationBoolean`.

- `escalationBoolean` is `true` when `confidenceScore < 0.70`
- `escalationBoolean` is also forced to `true` when `groundingSources` is empty

The second rule is deliberate. A model can overstate confidence. An answer
with no grounding sources is outside the knowledge boundary and must
escalate regardless of the reported score.

A business escalation is not an HTTP error — it returns `200 OK` with
`escalationBoolean: true`.

## HTTP Response Contract

| Status | Meaning |
|---|---|
| 200 OK | Successful request; check `escalationBoolean` for business outcome |
| 400 Bad Request | Request validation failed |
| 422 Unprocessable Entity | AI output or business-rule validation failed |
| 429 Too Many Requests | Rate limit exceeded |
| 503 Service Unavailable | Required system dependency unavailable at startup |
| 500 Internal Server Error | Unhandled exception caught by global middleware |

## Authentication

`POST /api/faq/ask` is unauthenticated — it handles anonymous Tier 1
questions. Quotation endpoints require JWT Bearer authentication and
database-backed client configuration.

## Deployment Boundary

The current Render deployment exposes all four modules:

- Database-backed via Neon (serverless PostgreSQL) — FAQ, Quotation,
  Tracking, and Risk Assessment are all live
- Startup migrations run automatically in Production via
  `db.Database.Migrate()` in `Program.cs`
- Bundled JSON knowledge base is deployed with the API for FAQ
- Scalar is available publicly for inspection and testing

## Resilience

- Retry with exponential backoff on every AI call
- 429 included in the retry predicate alongside default 5xx/408/timeout coverage
- Timeout handling
- Circuit-breaker behavior
- Rate limiting with per-IP partitioning
- Response caching for repeated FAQ-style questions
- Global exception handling

## Roadmap

| Phase | Description | Status |
|---|---|---|
| Phase 1: FAQ | Knowledge-grounded logistics FAQ | Live on Render |
| Phase 2: Quotation | Database-backed quotation workflow | Live on Render |
| Phase 2.5: Provider abstraction | Typed provider settings, `ILlmClient` abstraction | Completed |
| Phase 3: Tracking | Shipment tracking workflow | Live on Render |
| Phase 3.5: Delay/risk assessment | Aggregate lane-history comparison, deterministic risk level | Live on Render |
| Phase 4: Booking | Agentic booking workflow with Semantic Kernel | Planned |