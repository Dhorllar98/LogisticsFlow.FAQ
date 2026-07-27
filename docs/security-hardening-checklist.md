# Security Hardening Checklist — LogisticsFlow AI Suite

This is a living, project-wide checklist. Every item applies to all
current and future phases unless explicitly superseded. New phases add
rows — they do not replace or narrow prior security rules.

## 1. Input Validation

- [x] Every DTO has a FluentValidation validator before reaching a service
- [ ] Free-text fields have explicit length ceilings
- [x] Structured fields use allow-lists or enums, never loose string trust
- [ ] Failed validation returns 400 and is never silently coerced

## 2. AI Call Boundary

- [x] Every AI response is schema-validated before use
- [x] Malformed AI output returns 422, not a guessed response
- [ ] Self-reported model claims are never trusted as a security boundary
- [ ] `max_tokens` is set explicitly on every AI call
- [ ] Every AI call has retry, timeout, and circuit-breaker behavior
- [x] 429 is included in the retry predicate alongside default transient failures
- [ ] Every outbound AI `HttpClient` call has an explicit timeout

## 3. Sensitive Data Lifecycle

Active once a module handles Tier 2 or Tier 3 data.

- [x] Tier 2 and Tier 3 fields enumerated explicitly per module before coding starts
- [x] Redaction map exists only for request lifetime — never cached, logged, or persisted
- [x] Restore failure returns hard 422, never a partial or leaked response
- [ ] Logs, cache keys, external calls, and exception paths audited for pre-redaction exposure

## 4. Exception Handling

- [x] Global middleware catches unhandled exceptions
- [x] Controllers do not manually construct generic 500 responses
- [x] Domain exceptions map to deliberate status codes
- [ ] Logged exception detail respects redaction rules
- [ ] Tier 2 or Tier 3 content never appears in stack traces or structured logs

## 5. Rate Limiting and Abuse Prevention

- [x] Per-IP rate limiting applied to public endpoints
- [x] Rate limiter placed before authentication middleware
- [ ] Endpoint limits tuned by cost and sensitivity
- [ ] Per-account limiting added once account/client identity exists
- [x] 429 responses tested, including `Retry-After` header presence

## 6. Logging and Observability

- [ ] Structured logging exists on important service boundaries
- [ ] AI calls log latency and failure mode
- [ ] AI calls do not log sensitive prompt or response content
- [ ] Redaction and restore outcomes logged as success/failure only
- [ ] Provider selection and failover events are observable

## 7. Pre-Deployment Checklist

- [ ] Integration tests simulate AI timeout
- [x] Integration tests simulate 429 handling
- [ ] Integration tests simulate malformed AI JSON
- [x] Tests assert Tier 2/3 fields never appear unredacted in outbound payloads
- [x] Tests assert broken redaction maps return 422
- [ ] Rate limiter is load-tested, not only unit-tested
- [x] GitLeaks or equivalent secret scanning run before pushing

## 8. Response Headers

- [x] `X-Content-Type-Options: nosniff` set on every response
- [x] `X-Frame-Options: DENY` set on every response
- [x] `Referrer-Policy: strict-origin-when-cross-origin` set on every response
- [x] `Permissions-Policy` denies geolocation, microphone, and camera on every response
- [x] Headers apply to error responses and the Scalar API explorer, not only successful controller results
- [x] Verified present on a live production response (Scalar API explorer endpoint)

## Phase Status

| Phase | Sections Active | Public Demo Status | Notes |
|---|---|---|---|
| Phase 1: FAQ | 1, 2, 4, 5, 6, 7, 8 | Live on Render | No Tier 2/3 data; section 3 not applicable |
| Phase 2: Quotation | 1, 2, 3, 4, 5, 6, 7, 8 | Built, not in public demo | Requires persistent database; Tier 2 redact/restore lifecycle active; IDOR fix applied (AccountId scoped via JWT claims, not client input); multi-agreement resolution added post-launch, AgreementId always scoped to the caller's own account, never a bare-ID lookup |
| Phase 2.5: Hardening | 1, 2, 4, 5, 6, 7 | Completed | JWT added, rate-limit policies split, provider-agnostic settings, middleware order fixed |
| Phase 3: Tracking | 1, 2, 3, 4, 5, 6, 7, 8 | Live on Render | Tier 2 redact/restore lifecycle active (tracking number, account ID, shipment addresses, consignee); single-call, non-agentic v1 per CLAUDE.md, SK exception holds until Phase 3.5. Tech debt closed: `ShipmentMode` converted from string to enum (Domain/Infrastructure); exception→status-code mapping for `LlmRateLimitException`/`LlmTimeoutException`/`LlmInvalidResponseException` confirmed already correctly wired to 429/504/502 in `GlobalExceptionMiddleware`. |
| Phase 3.5: Risk Assessment | 1, 2, 3, 4, 5, 6, 7, 8 | Live on Render | Reviewed for SK adoption at kickoff, found non-agentic (deterministic data pipeline, no dynamic tool selection) — SK deferred to Phase 4. Aggregate-only lane comparison: pooled, depersonalized statistics across all clients on a lane, grouped by coarse region (never exact address), minimum sample size of 5 before surfacing an average. Risk level computed deterministically in C#, never AI-decided. No new Tier 2 fields; new Tier 1 derived fields only (region, aggregate stats). 84 tests passing project-wide. |
| HTTP Security Headers | 8 | Live on Render | Closed - was the one open item carried from Phase 3's original tech-debt review through Phase 3.5. `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` added via dedicated middleware registered immediately after global exception handling. Confirmed present on a live production response. |
| Phase 4: Booking | TBD at kickoff | Planned | Agentic workflow; genuine dynamic tool selection (carrier/date/pricing fallback), Infinite Loop Guard and tool-output trust controls become active for the first time |
| Multi-provider: Gemini | 1, 2 | Live on Render | Second manually-selectable provider (`GeminiApiClient`), same resilience posture as Claude (timeout, 429/error handling, structured logging). `HttpRequestException` (raw connection failures) mapped to 502 in `GlobalExceptionMiddleware`, previously fell through to the generic 500 catch-all. |