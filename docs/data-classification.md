# Data Classification — LogisticsFlow FAQ Assistant

## The Three-Tier System

| Tier | Definition | AI Processing Rule |
|---|---|---|
| Tier 1: Public | Non-sensitive, freely shareable information | Cloud LLMs permitted |
| Tier 2: Internal | Business-sensitive data: client accounts, shipment records, negotiated rates | Redact before cloud LLM calls; restore after |
| Tier 3: Restricted | PII, payment data, credentials | Local processing only — never sent to a cloud LLM |

## Classification of the FAQ Module

The entire FAQ knowledge base is classified Tier 1.

Every entry describes general logistics knowledge: shipping terminology,
regulatory definitions, standard calculation formulas, public service
descriptions, and operational concepts. None of it identifies a specific
customer, contains a negotiated rate, or includes personal or financial
data.

This classification is declared explicitly in `FAQService`'s processing
flow even though no enforcement action is triggered by it. This establishes
the audit pattern before later modules introduce data that requires Tier 2
or Tier 3 handling.

## Trust vs. Sensitivity

The tier system classifies data sensitivity: what the data is and where it
may be processed. It does not classify data trust: whether the content may
be attacker-controlled or may attempt to manipulate system behavior.

A Tier 1 user query is still untrusted input. The FAQ module treats the
user's free-text query as untrusted and handles it accordingly — see
`docs/architecture.md` for the input-boundary controls.

## FAQ Module Boundary

The current public FAQ module answers only general, anonymous logistics
questions. If a future enhancement allowed a user to reference a specific
shipment, account, negotiated rate, or customer record, that request would
become Tier 2 immediately.

## Classification by Module

| Module | Expected Classification | Notes |
|---|---|---|
| FAQ | Tier 1 | Live public demo; general logistics knowledge only |
| Quotation | Tier 2 | Client-specific accounts and negotiated rates |
| Tracking | Tier 2 | Tracking numbers tied to customer accounts |
| Booking | Tier 2 and Tier 3 | May touch customer details, payment data, and credentials |

## Current Public Demo

The current Render deployment stays inside the Tier 1 boundary. Quotation
functionality exists in the codebase but is not enabled in the public demo
because it requires persistent database infrastructure and Tier 2 controls.

## Secrets Handling

API keys are stored exclusively in environment variables. `appsettings.Development.json`
is excluded via `.gitignore`. Secret scanning should be run before pushing
to the remote repository.