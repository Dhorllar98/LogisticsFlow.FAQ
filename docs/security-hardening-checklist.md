# Security Hardening Checklist — LogisticsFlow AI Suite

This is a living, project-wide checklist. Every item applies to all
current and future phases unless explicitly superseded. New phases add
rows — they do not replace or narrow prior security rules.

## 1. Input Validation

- [ ] Every DTO has a FluentValidation validator before reaching a service
- [ ] Free-text fields have explicit length ceilings
- [ ] Structured fields use allow-lists or enums, never loose string trust
- [ ] Failed validation returns 400 and is never silently coerced

## 2. AI Call Boundary

- [ ] Every AI response is schema-validated before use
- [ ] Malformed AI output returns 422, not a guessed response
- [ ] Self-reported model claims are never trusted as a security boundary
- [ ] `max_tokens` is set explicitly on every AI call
- [ ] Every AI call has retry, timeout, and circuit-breaker behavior
- [ ] 429 is included in the retry predicate alongside default transient failures
- [ ] Every outbound AI `HttpClient` call has an explicit timeout

## 3. Sensitive Data Lifecycle

Active once a module handles Tier 2 or Tier 3 data.

- [ ] Tier 2 and Tier 3 fields enumerated explicitly per module before coding starts
- [ ] Redaction map exists only for request lifetime — never cached, logged, or persisted
- [ ] Restore failure returns hard 422, never a partial or leaked response
- [ ] Logs, cache keys, external calls, and exception paths audited for pre-redaction exposure

## 4. Exception Handling

- [ ] Global middleware catches unhandled exceptions
- [ ] Controllers do not manually construct generic 500 responses
- [ ] Domain exceptions map to deliberate status codes
- [ ] Logged exception detail respects redaction rules
- [ ] Tier 2 or Tier 3 content never appears in stack traces or structured logs

## 5. Rate Limiting and Abuse Prevention

- [ ] Per-IP rate limiting applied to public endpoints
- [ ] Rate limiter placed before authentication middleware
- [ ] Endpoint limits tuned by cost and sensitivity
- [ ] Per-account limiting added once account/client identity exists
- [ ] 429 responses tested, including `Retry-After` header presence

## 6. Logging and Observability

- [ ] Structured logging exists on important service boundaries
- [ ] AI calls log latency and failure mode
- [ ] AI calls do not log sensitive prompt or response content
- [ ] Redaction and restore outcomes logged as success/failure only
- [ ] Provider selection and failover events are observable

## 7. Pre-Deployment Checklist

- [ ] Integration tests simulate AI timeout
- [ ] Integration tests simulate 429 handling
- [ ] Integration tests simulate malformed AI JSON
- [ ] Tests assert Tier 2/3 fields never appear unredacted in outbound payloads
- [ ] Tests assert broken redaction maps return 422
- [ ] Rate limiter is load-tested, not only unit-tested
- [ ] GitLeaks or equivalent secret scanning run before pushing

## Phase Status

| Phase | Sections Active | Public Demo Status | Notes |
|---|---|---|---|
| Phase 1: FAQ | 1, 2, 4, 5, 6, 7 | Live on Render | No Tier 2/3 data; section 3 not applicable |
| Phase 2: Quotation | 1, 2, 3, 4, 5, 6, 7 | Built, not in public demo | Requires persistent database; Tier 2 redact/restore lifecycle active |
| Phase 2.5: Hardening | 1, 2, 4, 5, 6, 7 | Completed | JWT added, rate-limit policies split, provider-agnostic settings, middleware order fixed |
| Phase 3: Tracking | 1, 2, 3, 4, 5, 6, 7 | Not started — scope locked | Tier 2 redact/restore lifecycle active (tracking number, account ID, shipment addresses, consignee); Option A single-call non-agentic v1 per CLAUDE.md, SK exception holds until Phase 3.5 |
| Phase 4: Booking | TBD at kickoff | Planned | Agentic workflow; tool-output trust controls become active |