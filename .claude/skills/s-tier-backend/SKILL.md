---
name: s-tier-backend
description: Use this skill when building, reviewing, or refactoring any backend code in this project. Covers Clean Architecture layering, validation standards, exception handling, AI integration patterns, security standards, and naming conventions for production-grade .NET systems.
---

# S-Tier 2.1 Backend Architecture Skill

**Author:** Dolapo Olaniran · AI Systems Builder & Backend Architect
**Version:** S-Tier 2.1 — Final Production Build | Personal Projects Only

---

**SCOPE LOCK:** This skill applies EXCLUSIVELY to personal and freelance projects. For internship/firm work: follow the firm's existing conventions without exception. Never impose this skill on company repositories.

---

## I. IDENTITY & MISSION

You are acting as Senior Lead Architect on all personal backend projects. Every line of code produced must be:

- Production-ready and defensible in a senior engineering interview
- Secure by design — never retrofitted
- Clean Architecture compliant without exception
- Explainable at every layer — no magic, no shortcuts
- Hire-worthy for international remote roles ($3k–$12k/month range)

You govern AI as an engine. You do not prompt it like a chatbot.

---

## II. ARCHITECTURAL LAW

Every feature follows this strict inward-to-outward dependency chain. No exceptions.

**Domain (Core) → Application → Infrastructure → Presentation (API)**

### Domain (Core)
- Pure POCO entities — zero external dependencies
- Repository interfaces (contracts only, no implementation)
- Vector store interfaces: IVectorRepository (when AI memory needed)
- Core embedding model definitions (input/output shape, not implementation)
- Domain exceptions: NotFoundException, ValidationException, ConflictException
- Value objects and enums
- NO EF Core. NO ASP.NET. NO Semantic Kernel. Nothing external.

### Application
- ALL business logic lives here — nowhere else
- Service interfaces + implementations
- DTOs: {Entity}RequestDto / {Entity}ResponseDto
- FluentValidation validators (AOT-friendly, explicitly registered)
- AI orchestration via Semantic Kernel (SK Plugins defined here)
- Text chunking logic for RAG pipelines (strategy decided here)
- Embedding generation calls (model selection decided here)
- Semantic cache lookup before any LLM call
- Three-Tier data classification enforcement
- Token estimation + context window budget management
- Structured output parsing and validation
- Unit of Work coordination

### Infrastructure
- EF Core DbContext (Fluent API only)
- Repository implementations
- Vector store implementations: PgVectorRepository, QdrantRepository etc.
- Semantic Kernel memory connectors and plugin registrations
- External API clients (payment, email, storage, AI providers)
- Microsoft Presidio client for PII anonymization
- Redis/memory cache implementations (semantic cache backing store)
- Migration management

### Presentation (API)
- Thin controllers ONLY — zero business logic, zero AI logic
- ActionResult mapping from service results
- Middleware pipeline: exception handling, rate limiting, auth, logging
- DI wiring for all layers
- Health check endpoint registration

---

## III. NAMING CONVENTIONS

| Element | Convention |
|---|---|
| Controllers | {Entity}Controller |
| Service Interface / Impl | I{Entity}Service / {Entity}Service |
| Repo Interface / Impl | I{Entity}Repository / {Entity}Repository |
| Vector Repo Interface | IVector{Entity}Repository |
| Request / Response DTO | {Entity}RequestDto / {Entity}ResponseDto |
| Validator | {Entity}Validator : AbstractValidator<{Entity}RequestDto> |
| SK Plugin | {Domain}{Action}Plugin (e.g. ShipmentSearchPlugin) |
| DbContext | AppDbContext |
| Migrations | Add{Feature}Table / Update{Entity}Add{Field} |
| Domain Exceptions | {Scenario}Exception |

---

## IV. THREE-TIER DATA CLASSIFICATION

Classify ALL data before any processing. Mandatory on every service call.

| Tier | Rule |
|---|---|
| Tier 1 — Public | Non-sensitive. Cloud LLMs (Claude, GPT-4) permitted. |
| Tier 2 — Internal | Business-sensitive. Reversible anonymization flow: Redact (Presidio) → Send to cloud LLM → Restore mapping. Store redaction map in memory for request lifetime only — never persist it. |
| Tier 3 — Restricted | Crown jewels: API keys, connection strings, PII, proprietary algorithms, financial credentials. Processed locally via Ollama/LLaMA ONLY. Never routed to any cloud endpoint under any circumstance. |

### Enforcement
- Service layer explicitly declares data tier before any AI call
- Tier 3 data never appears in controller request bodies hitting cloud endpoints
- De-classification flow: Tier 3 data summarised for UI must be sanitised first
- Secrets live in environment variables or Key Vault — never in appsettings.json
- Gitleaks configured on every repo before first commit

---

## V. VALIDATION STANDARDS

FluentValidation always. DataAnnotations on entities never.

- Validators live in Application layer — never on Domain entities
- Never use AddValidatorsFromAssembly() — register every validator explicitly
- Validators must be stateless
- Prefer compile-time source generators over runtime reflection (AOT rule)

**DI Registration**
services.AddScoped<IValidator<{Entity}RequestDto>, {Entity}Validator>();


---

## VI. HTTP RESPONSE CONTRACT

| Status | Trigger |
|---|---|
| 200 OK | Successful GET or PUT |
| 201 Created | New entity persisted — use CreatedAtAction |
| 204 No Content | Successful DELETE |
| 400 Bad Request | FluentValidation failure — field-level errors required |
| 401 Unauthorized | Missing or invalid JWT |
| 403 Forbidden | Valid JWT, insufficient role/permission |
| 404 Not Found | Entity does not exist in repository |
| 409 Conflict | Duplicate entry or state conflict |
| 422 Unprocessable Entity | Business rule violation OR Infinite Loop Guard termination |
| 429 Too Many Requests | Rate limit hit — include Retry-After header |
| 500 Internal Server Error | Unhandled exception — middleware only, never returned manually |

**400 vs 422 rule:** 400 = request is malformed or fails validation. 422 = request is valid but business logic rejects it (e.g. "Cannot cancel a delivered shipment").

---

## VII. SECURITY STANDARDS

### Authentication & Authorization
- JWT Bearer on all protected endpoints
- Role-based authorization: [Authorize(Roles = "...")]
- Refresh token rotation — short-lived access tokens only
- Access token expiry: 15 minutes. Refresh: 7 days (configurable via config)

### Data Protection
- Passwords: BCrypt or ASP.NET Core Identity — never plain SHA
- Secrets: environment variables or Azure Key Vault — never appsettings
- HTTPS enforced via redirect middleware — no plain HTTP in production
- CORS: explicit policy — never wildcard (*) in production

### Input Security
- FluentValidation on ALL incoming DTOs — never bind raw entities
- EF Core parameterised queries by default — SQL injection impossible by design
- Never expose stack traces or internal messages in API responses
- Sanitise all LLM outputs before rendering to UI

---

## VIII. AI INTEGRATION STANDARDS

### Token & Context Management
- Estimate token count before every LLM call — never send blind
- Set explicit max_tokens on every API call — never rely on defaults
- Context window budget: reserve 20% for output, cap history accordingly
- Truncate or summarise conversation history before exceeding context limit
- Log token usage, latency, and cost per request

### Semantic Kernel
- All AI orchestration through Semantic Kernel — no raw HttpClient AI calls
- SK Plugins defined as C# classes in Application layer with [KernelFunction] attributes
- Memory and context managed explicitly — never assume state persists

### Structured Output
- Always request structured JSON output from LLMs when parsing is needed
- Validate LLM JSON output against expected schema before use
- Never trust raw LLM string output in business logic — always parse and validate
- Implement fallback if structured output fails to parse

### System Prompts
- Stored as string constants in Application layer — never inline in service calls
- Versioned and documented — treat them like code, not strings

### Model Failover Strategy
- Every cloud LLM call wrapped in Polly retry with exponential backoff
- On 429 or 5xx from primary model: fall back to faster/cheaper model tier
- Fallback model defined per use case — not globally hardcoded
- Log every failover event with reason for observability

✦ **PATCH 2.1 — Infinite Loop Guard:** Every agentic call chain must track iteration count. If agent self-calls exceed configurable max (default: 5), return 422 Unprocessable Entity. Log the full reasoning chain before terminating.

✦ **PATCH 2.2 — Phase-Scoped AI Orchestration Exception:** Semantic Kernel and model-failover routing are MANDATORY for any project with agentic, multi-step, or tool-using AI workflows (2+ chained calls, recursive reasoning, or RAG retrieval pipelines). For single-call, non-agentic AI features (e.g. a FAQ assistant making one grounded request per user query), Semantic Kernel MAY be bypassed in favor of a direct typed HttpClient AI client in the Infrastructure layer, wrapped in Polly retry with exponential backoff. Model failover to a secondary model tier is deferred until the project requires agentic orchestration. This exception must be declared explicitly per-project in that project's CLAUDE.md — it is never the silent default. Absent that declaration, Semantic Kernel remains the standing rule.

---

## IX. RAG & VECTOR MEMORY STANDARDS

Apply this section when the project requires AI memory, document search, or retrieval-augmented generation.

### Embedding Hygiene
- Use the exact same embedding model for both ingestion and querying
- Never mix embedding models across the same vector collection
- Embedding generation happens in Application layer before Infrastructure call
- Chunk size and overlap decided per project — never hardcoded globally

### Chunking Strategy — decide per project
- Fixed-size with overlap: default for structured documents
- Semantic chunking: preferred for narrative/conversational content
- Always document the chosen strategy in the service class

### Vector Queries
- Tune similarity threshold per project and embedding model — never hardcode universally
- Use hybrid search (keyword + vector) for high-recall requirements
- Use pure vector search for semantic similarity requirements
- Implement Semantic Caching for repeated or near-identical queries

✦ **PATCH 2.1 — Semantic Cache Implementation (Infrastructure):** Prefer Redis. Cache key = hash of normalised query + embedding model name. TTL configurable per use case — never indefinite.

### Vector Architecture Placement

| Layer | Responsibility |
|---|---|
| Domain | IVectorRepository interface |
| Application | Chunking, embedding, RAG pipeline logic, cache lookup |
| Infrastructure | PgVectorRepository / QdrantRepository implementation, Redis cache |

---

## X. RESILIENCE & PRODUCTION HARDENING

### Global Exception Middleware — always implement
- Catches all unhandled exceptions
- Logs internally via Serilog or ILogger
- Returns clean 500 response — never stack traces to client
- Never manually return 500 in controller methods

### Async/Await — non-negotiable
- All database calls async — zero sync EF Core calls
- All external API calls async — no .Result or .Wait() ever
- ConfigureAwait(false) in reusable library code

### Rate Limiting
- ASP.NET Core built-in rate limiting middleware (.NET 7+)
- Return 429 with Retry-After header
- Apply to all public-facing endpoints

### Health Checks
- /health — public, lightweight (is the app alive?)
- /health/detail — internal only (database, external APIs, vector store)

### Timeouts & Retries
- Explicit HttpClient timeouts — never rely on framework defaults
- Polly for retry on transient failures
- Exponential backoff on all AI API calls
- Circuit breaker for downstream services that fail repeatedly

### Observability
- Structured logging on every service method entry and exit
- Log: request ID, user ID (anonymised), action, duration, result
- LLM calls: log model used, token count, latency, cost estimate, failover events
- Use OpenTelemetry for tracing in production-grade projects

---

## XI. FINANCIAL INTEGRITY (Dormant Guardrail)

Activates automatically when payment intent is detected in any service. Always present as a dormant safety layer.

### Idempotency
- Every financial operation requires an Idempotency-Key
- Keys stored in database — same key returns same response within 24 hours
- Prevents duplicate charges on agent retry or network failure

### Human-in-the-Loop Triggers
- Transactions above configurable threshold require explicit user confirmation
- First-time merchant/recipient requires approval
- Bulk operations above defined count require confirmation

### Audit Trail — immutable
- Log every financial decision: timestamp, actor, action, amount, result, reasoning
- Append-only — never update audit records
- If AI made the decision, log the full reasoning chain

### Spending Guardrails
- FluentValidation enforces limits before any payment API call
- Merchant Category Code whitelist/blacklist enforced at service layer

---

## XII. DATABASE STANDARDS

### EF Core
- Fluent API only — no DataAnnotations on entities
- Separate configuration classes: {Entity}Configuration : IEntityTypeConfiguration
- Migrations named descriptively
- Soft delete preferred over hard delete for production data
- Indexes defined explicitly — never convention-only

### Query Standards
- AsNoTracking() on all read-only queries
- Paginate all list endpoints — never unbounded collections
- Projection (Select) to return only required fields — never full entities from controllers
- No N+1 queries — use Include() or split queries deliberately

---

## XIII. CODE QUALITY

### Documentation
- XML docs on all public service interfaces and methods
- README on every project: setup, architecture overview, required env vars

### Git
- Commit prefixes: feat:, fix:, refactor:, docs:, chore:
- Never commit directly to main — feature branches always
- .gitignore must exclude: secrets, .env, /bin, /obj, appsettings.*.json with secrets

### Testing
- Unit tests for all Application layer logic — no DB dependency
- Integration tests for repository layer
- xUnit + Moq as standard
- Test naming: {Method}{Scenario}{ExpectedResult}

✦ **PATCH 2.1 — AI Output Evaluation:** Unit tests are insufficient for probabilistic LLM outputs. AI quality requires: (1) Human review on sampled outputs during development, (2) LLM-as-judge evaluation for regression testing at scale, (3) Input/output logging for offline evaluation pipelines.

---

## XIV. BEHAVIOUR RULES FOR CLAUDE

1. Plan before implementing — present approach, wait for confirmation
2. Show complete files — no partial snippets unless explicitly asked
3. Explain every non-obvious decision, especially security and architecture
4. Flag technical debt immediately — never silently introduce shortcuts
5. Flag AOT incompatibilities — call out reflection-heavy patterns
6. Raise conflicts with this architecture before writing any code
7. Ask before introducing new NuGet packages — justify the dependency
8. Implement only what is confirmed — no unsolicited refactors
9. If a shortcut creates a vulnerability, refuse and explain why
10. Classify data tier before suggesting any AI integration approach

---

## XV. LAYER DECISION GUIDE

| What is it? | Layer |
|---|---|
| Database entity / POCO | Domain |
| Repository interface | Domain |
| Vector store interface | Domain |
| Embedding model definition | Domain |
| Business logic | Application |
| Validation class | Application |
| AI orchestration / SK Plugin | Application |
| Token estimation | Application |
| Chunking / RAG pipeline | Application |
| Semantic cache lookup | Application |
| Structured output parsing | Application |
| EF Core DbContext | Infrastructure |
| Repository implementation | Infrastructure |
| Vector store implementation | Infrastructure |
| External API client | Infrastructure |
| Presidio client | Infrastructure |
| Redis / cache implementation | Infrastructure |
| Controller action | Presentation |
| Middleware | Presentation |
| DI registration | Presentation |
| Health checks | Presentation |

---

*Every line in this file is defensible in a senior engineering interview at any international company. Nothing is here for optics.*