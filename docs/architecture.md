# Architecture — LogisticsFlow FAQ Assistant

## Overview
This service is a knowledge-grounded FAQ assistant for multimodal logistics
operations (Land, Sea, Air). It is built on Clean Architecture principles
with a strict inward-to-outward dependency chain, and uses a RAG-lite
pattern — direct knowledge base injection into the LLM context — rather
than a full vector database, appropriate for a curated, finite knowledge
set of this size.

## Dependency Chain

Domain → Application → Infrastructure → Presentation

Each layer depends only on the layer(s) inward of it. Domain has zero
external dependencies — no NuGet packages, no framework references.

### Domain (`LogisticsFlow.Domain`)
- `Entities/FAQEntry.cs` — core knowledge base record (Id, Category, Question, Answer)
- `Entities/ConversationSession.cs`, `Entities/ChatMessage.cs` — conversation state
- `Enums/LogisticCategory.cs` — Land, Sea, Air, General
- `Exceptions/BusinessRuleException.cs`, `Exceptions/KnowledgeBoundaryException.cs`
- `Interfaces/IFAQRepository.cs`, `Interfaces/IClaudeApiClient.cs` — contracts only

### Application (`LogisticsFlow.Application`)
- `DTOs/FAQRequestDto.cs`, `DTOs/FAQResponseDto.cs`
- `Validators/FAQRequestValidator.cs`, `Validators/FAQResponseValidator.cs` (FluentValidation)
- `Services/FAQService.cs` — orchestrates the request: load knowledge base,
  construct prompt, call AI client, validate and shape the response
- `Prompts/SystemPrompts.cs` — versioned system prompt as a string constant
- `Interfaces/IFAQService.cs`

### Infrastructure (`LogisticsFlow.Infrastructure`)
- `AI/ClaudeApiClient.cs` — typed `HttpClient` wrapper around the Claude API,
  wrapped in Polly retry with exponential backoff
- `Repositories/JsonFAQRepository.cs` — loads `data/faq_knowledgebase.json`
  at startup, cached in memory
- `Cache/FAQCacheService.cs` — `IMemoryCache`-backed response cache, keyed
  on normalized query, 24-hour TTL
- `DependencyInjection.cs` — explicit service registration

### Presentation (`LogisticsFlow.API`)
- `Controllers/FAQController.cs` — thin controller, single endpoint:
  `POST /api/faq/ask`
- `Middleware/GlobalExceptionMiddleware.cs`
- `Settings/ClaudeApiSettings.cs` — bound from environment/configuration

## AI Integration Approach: RAG-Lite

Rather than chunking, embedding, and vector-searching the knowledge base,
the full curated FAQ set (33 entries as of Phase 1) is injected directly
into the system prompt on every request. This is appropriate because the
knowledge base is small, finite, and fully curated — a vector store would
add infrastructure cost and latency with no retrieval-quality benefit at
this scale. Vector search becomes appropriate once the knowledge base grows
beyond what reliably fits in context, which is a Phase 2+ consideration.

## Semantic Kernel: Phase-Scoped Exception

Per Patch 2.2 of the governing architecture skill, Semantic Kernel is
**not used** in this phase. The FAQ flow is a single, non-agentic AI call —
one user query produces one grounded response, with no chained reasoning
or tool-calling. Semantic Kernel becomes mandatory starting with any module
that introduces multi-step or agentic workflows (anticipated at the
Booking module).

## Confidence and Escalation Logic

Every response includes a `ConfidenceScore` (0.0–1.0) self-assessed by the
model, plus a list of `GroundingSources` citing which knowledge base
entries were used.

- `EscalationBoolean` is set to `true` when `ConfidenceScore < 0.70`
- `EscalationBoolean` is **also** forced to `true` whenever
  `GroundingSources` is empty, regardless of the reported confidence score —
  an empty source list means the model answered outside the knowledge
  boundary, which the self-reported score alone cannot be trusted to flag

This dual check exists because LLM self-assessed confidence is an
unreliable single signal; grounding evidence is the more trustworthy check.

## HTTP Response Contract

| Status | Meaning in this service |
|---|---|
| 200 OK | Successful request — check `EscalationBoolean` in the body for business outcome |
| 400 Bad Request | FluentValidation failure on the incoming request |
| 422 Unprocessable Entity | System-level failure only (e.g. malformed AI output that fails schema validation) |
| 429 Too Many Requests | Rate limit exceeded |
| 500 Internal Server Error | Unhandled exception, caught by global middleware |

Critically: a business decision to escalate to a human is **not** an error.
It returns `200 OK` with `EscalationBoolean: true` in the payload. `422` is
reserved for genuine system failures, never for "the AI didn't know."

## Resilience

- Every Claude API call is wrapped in Polly retry with exponential backoff
- Rate limiting: 20 requests per IP per minute on `/api/faq/ask`
- Repeated FAQ-style queries are served from cache, reducing both latency
  and Claude API cost

## Roadmap

This module is Phase 1 of four: FAQ → Quotation → Order Tracking →
Bookings. Later modules will introduce agentic, multi-step AI workflows
(triggering the Semantic Kernel requirement) and will touch Tier 2/3 data
(see `data-classification.md`).