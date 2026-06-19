# LogisticsFlow FAQ Assistant — Claude Code Context

## Architecture Law
Clean Architecture: Domain → Application → Infrastructure → Presentation.
Domain has zero external dependencies. Controllers are thin. All business
logic lives in Application Services.

## Phase 1 Scope Exception (see s-tier-backend skill, Patch 2.2)
This phase is a single-call, non-agentic AI feature. Per the Phase-Scoped
AI Orchestration Exception, Semantic Kernel is NOT used here. AI calls go
through a direct typed HttpClient client in Infrastructure, wrapped in
Polly retry with exponential backoff. This exception holds only while FAQ
remains single-call/non-agentic; any chained or tool-using step (e.g. a
clarification round-trip before answering) reinstates the Semantic Kernel
requirement.

## Phase 1 Constraints
- No Semantic Kernel, no Presidio, no model failover yet
- Confidence threshold: 0.70
- Empty GroundingSources forces EscalationBoolean: true regardless of score
- 422 = system/infra failure only. Business escalation = 200 OK + 
  EscalationBoolean: true in the response body
- Rate limiting: 20 requests/IP/minute on /api/faq/ask
- CORS: named policy — Vercel production domain + localhost dev

## Tech Stack
.NET 8, ASP.NET Core, FluentValidation, Polly, Serilog, React 18, 
TypeScript, Tailwind CSS

## Naming Conventions
DTOs: {Entity}RequestDto / {Entity}ResponseDto
Validators: {Entity}Validator
Interfaces: I{Name}
Services: {Entity}Service

## Reference
Full architecture standards live in .claude/skills/s-tier-backend/SKILL.md.
Defer to it for anything not covered above.