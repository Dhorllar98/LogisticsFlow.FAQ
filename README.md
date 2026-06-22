# LogisticsFlow AI: Knowledge-Grounded FAQ Assistant

A production-grade, RAG-lite AI assistant that handles tier-1 customer
support inquiries for multimodal logistics operations — Land, Sea, and
Air freight — built on Clean Architecture with explicit data governance
and AI resilience patterns.

## The Problem

Logistics support teams are burdened by high-volume, repetitive inquiries
about transit times, customs procedures, freight terminology, and pricing
rules. This leads to support fatigue, slow response times, and routine
questions consuming time that should go toward complex client issues.

## The Solution

This assistant answers customer questions using a curated, 33-entry
knowledge base spanning Land, Sea, Air, and general logistics operations.
Unlike a generic chatbot, every response is grounded in that knowledge
base — if a question falls outside its boundary, the system recognizes
this and hands off to a human, rather than guessing.

## Architecture

Built on Clean Architecture (Domain → Application → Infrastructure →
Presentation), with a deliberate, documented exception to Semantic Kernel
for this phase, since the workflow is single-call rather than agentic.
Full reasoning is in [`docs/architecture.md`](docs/architecture.md).

## Key Features

- **Grounded responses**: every answer cites which knowledge base entries
  it drew from
- **Confidence-aware escalation**: low-confidence or ungrounded responses
  automatically route to a human support CTA rather than guessing
- **Production hardening**: rate limiting, retry with exponential backoff,
  response caching, and global exception handling are implemented from
  day one, not retrofitted later

## Security Considerations

- **Data classification by design**: every category of data this system
  processes is explicitly classified Tier 1/2/3 before any AI call is
  made — see [`docs/data-classification.md`](docs/data-classification.md)
- **Prompt-injection resistance**: user input is structurally separated
  from the system prompt at the API call boundary, never templated into
  it — see [`docs/architecture.md`](docs/architecture.md#input-handling-prompt-injection-resistance)
- **Self-reported AI confidence is never trusted as a security boundary**:
  the escalation logic treats empty grounding sources as an automatic
  escalation trigger regardless of the model's stated confidence
- **Secrets management**: API keys live exclusively in environment
  variables, never in committed configuration files

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core Web API, FluentValidation, Polly, Serilog
- **Frontend**: React 18, TypeScript, Tailwind CSS v4, Vite
- **AI**: Claude API (Sonnet)