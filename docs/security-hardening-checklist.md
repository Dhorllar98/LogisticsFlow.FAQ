
# Security Hardening Checklist — LogisticsFlow AI Suite

This is a living, project-wide checklist. Every item, once added, applies

to ALL current and future phases unless explicitly superseded. New phases

ADD rows; they do not replace or scope-limit prior rows. Before writing

code for any phase, confirm every applicable item below against the

phase's CLAUDE.md section.

## 1. Input Validation

- [ ] Every DTO has a FluentValidation validator before reaching a service

- [ ] Free-text fields have explicit length ceilings

- [ ] Structured fields use allow-lists/enums, never loose string trust

- [ ] Failed validation rejects (400) — never silently coerces

## 2. AI Call Boundary

- [ ] Every Claude response is schema-validated before use; malformed = 422

- [ ] Self-reported model claims (confidence, "this is redacted") are

      never trusted as a security boundary

- [ ] max_tokens set explicitly on every call

- [ ] Every AI call wrapped in Polly retry + circuit breaker

- [ ] Explicit timeout on every HttpClient call

## 3. Sensitive Data Lifecycle (Tier 2/3 — active from Phase 2 onward)

- [ ] Tier 2/3 fields enumerated explicitly per module in that phase's

      CLAUDE.md section before coding starts

- [ ] Redaction map exists only for request lifetime — never cached,

      logged, or persisted (see CLAUDE.md -> Redaction/Restore Lifecycle)

- [ ] Restore failure -> hard 422, never a partial/leaked response

- [ ] Audit every log/cache/external-call site for pre-redaction exposure

## 4. Exception Handling

- [ ] Global middleware catches everything — no manual 500s in controllers

- [ ] Domain exceptions map to deliberate status codes, not generic catch-all

- [ ] Logged exception detail still respects redaction rules (no Tier 2/3

      in stack traces)

## 5. Rate Limiting & Abuse Prevention

- [ ] Per-IP limiting on every public endpoint, tuned per endpoint

      cost/sensitivity

- [ ] Per-account limiting once any account/client identification exists

## 6. Logging & Observability

- [ ] Structured logging on every service method entry/exit

- [ ] AI calls log token count, latency, failover events

- [ ] Redaction/restore outcomes logged (success/fail only — never content)

## 7. Pre-Deployment Checklist

- [ ] Integration tests simulate AI timeout / 429 / malformed JSON

- [ ] Test asserts Tier 2/3 fields never appear unredacted in outbound

      payloads

- [ ] Test asserts broken redaction map -> 422, not leaked partial response

- [ ] Rate limiter load-tested, not just unit-tested

- [ ] GitLeaks wired into pre-commit hook, not manual habit

---

## Phase Status

| Phase | Sections Active | Notes |

|---|---|---|

| 1 — FAQ | 1, 2, 4, 5, 6, 7 | No Tier 2/3 data — section 3 not applicable |

| 2 — Quotation | 1, 2, 3, 4, 5, 6, 7 | Section 3 now active — Presidio redact/restore lifecycle, see CLAUDE.md |

| 2.5 — Hardening | 1, 2, 4, 5, 6, 7 | JWT auth added to Quotation, rate-limit policies split, 429 retry + Retry-After fixed via real load testing |

| 3 — Tracking | TBD at kickoff | |

| 4 — Booking | TBD at kickoff | SK + tool-output trust become active |

