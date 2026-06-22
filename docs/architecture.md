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
  wrapped in a standard resilience handler (retry, circuit breaker, timeout)
- `Repositories/JsonFAQRepository.cs` — loads `data/faq_knowledgebase.json`
  at startup, cached in memory
- `Cache/FAQCacheService.cs` — `IMemoryCache`-backed response cache, keyed
  on normalized query, 24-hour TTL
- `DependencyInjection.cs` — explicit service registration

### Presentation (`LogisticsFlow.API`)
- `Controllers/FAQController.cs` — thin controller, single endpoint:
  `POST /api/faq/ask`
- `Middleware/GlobalExceptionMiddleware.cs`
- `Extensions/RateLimitingExtensions.cs`, `Extensions/CorsExtensions.cs` —
  cross-cutting concern wiring, kept out of `Program.cs`
- `Settings/ClaudeApiSettings.cs` — bound from environment/configuration

## AI Integration Approach: RAG-Lite

Rather than chunking, embedding, and vector-searching the knowledge base,
the full curated FAQ set (33 entries as of Phase 1) is injected directly
into the system prompt on every request. This is appropriate because the
knowledge base is small, finite, and fully curated — a vector store would
add infrastructure cost and latency with no retrieval-quality benefit at
this scale. Vector search becomes appropriate once the knowledge base grows
beyond what reliably fits in context, which is a Phase 2+ consideration.

## Input Handling: Prompt-Injection Resistance

The FAQ module accepts one untrusted input: the user's free-text query. It
is untrusted in the sense that it is attacker-controllable, not because it
is sensitive data — see `data-classification.md` for why those are
different axes.

Because this is a single-call, non-agentic flow, the threat surface is
narrow: there is no tool-calling loop, no agent re-reading external
content, and no chained reasoning step for an injected instruction to
hijack. The realistic attack here is a user query that attempts to
override the system prompt directly — e.g. instructing the model to
ignore the knowledge base, fabricate a high confidence score, or disclose
system instructions.

Controls for this phase:

- The user query is always passed as a discrete user-turn message, never
  concatenated into or templated inside the system prompt string. The
  system prompt (knowledge base + grounding rules) and the user query are
  structurally separated at the API call boundary.
- `FAQRequestValidator` rejects queries exceeding 500 characters and
  queries containing no alphabetic content, both common injection-padding
  patterns, before the request reaches the AI client. The same length and
  emptiness checks apply to each entry in conversation history, and history
  itself is capped at 6 entries — both at the validator and again at the
  `ConversationSession` entity level — to prevent an oversized or padded
  history from being used to bypass the per-query length ceiling.
- `ConfidenceScore` and `GroundingSources` are still self-reported by the
  model and are therefore not trusted as a security boundary — the
  existing dual-check escalation logic (empty sources forces escalation
  regardless of score) already provides partial resistance to a
  successful injection, since a hijacked response with no real grounding
  sources still escalates rather than returning a fabricated answer with
  artificially inflated confidence.
- Claude's own model-level instruction-hierarchy training is the primary
  defense against the system prompt being overridden by user input; this
  module does not attempt to reimplement that defense, only to avoid
  weakening it (e.g. by string-concatenating user input into the system
  prompt, which would blur the boundary Claude relies on).

**This is not yet the agentic threat model.** Once the Booking module
introduces Semantic Kernel and tool-calling (per Patch 2.2), a different
and more serious class of injection becomes live: tool outputs (carrier
API responses, availability-check results, error logs) becoming inputs to
further agent reasoning, where injected instructions inside that data
could be misread as commands. That control — treating all tool output as
untrusted data, never as instructions — is specified as a standing rule in
`CLAUDE.md` now, ahead of that module's implementation, rather than
retrofitted once Booking exists.

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
| 422 Unprocessable Entity | Business-rule/AI-output failure — malformed AI JSON, unrecognized category, or AI response failing schema validation. The original client request was valid; the AI's output could not be trusted. |
| 429 Too Many Requests | Rate limit exceeded (per-IP, see Resilience section) |
| 503 Service Unavailable | The knowledge base itself failed to load or parse at startup (`KnowledgeBoundaryException`) — a system-level dependency failure, distinct from a single query falling outside the knowledge boundary, which is a normal business outcome handled via `EscalationBoolean`, not an exception |
| 500 Internal Server Error | Any other unhandled exception, caught by global middleware |

Critically: a business decision to escalate to a human is **not** an error.
It returns `200 OK` with `EscalationBoolean: true` in the payload. `422` is
reserved for genuine AI-output failures, `503` for the knowledge base
itself being unavailable — neither is used for "the AI didn't know."

## Resilience

- Every Claude API call is wrapped in a standard resilience handler
  (retry with exponential backoff, circuit breaker, timeout)
- Rate limiting: 20 requests per IP per minute on `/api/faq/ask`,
  partitioned per-client-IP — not a shared global limit
- Repeated FAQ-style queries are served from cache, reducing both latency
  and Claude API cost

## Roadmap

This module is Phase 1 of four: FAQ → Quotation → Order Tracking →
Bookings. Later modules will introduce agentic, multi-step AI workflows
(triggering the Semantic Kernel requirement) and will touch Tier 2/3 data
(see `data-classification.md`).

4. **Bookings** — agentic, multi-step workflow (introduces Semantic Kernel).
   Architecture pattern: hierarchical orchestrator coordinating specialist
   sub-agents (e.g. quote generation, availability check, confirmation),
   per Anthropic's 2026 Agentic Coding Trends Report (Fountain case study —
   weeks-to-72-hours staffing time reduction via the same pattern).