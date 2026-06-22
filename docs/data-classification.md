# Data Classification — LogisticsFlow FAQ Assistant

## The Three-Tier System

| Tier | Definition | AI Processing Rule |
|---|---|---|
| Tier 1 — Public | Non-sensitive, freely shareable information | Cloud LLMs (Claude) permitted |
| Tier 2 — Internal | Business-sensitive (client accounts, negotiated rates) | Redact via Presidio before cloud LLM call, restore after |
| Tier 3 — Restricted | Crown jewels: PII, payment data, credentials | Local processing only (Ollama/Llama 3.1), never sent to any cloud endpoint |

## Classification of This Module

**The entire FAQ knowledge base is classified Tier 1.**

Justification: every entry in `faq_knowledgebase.json` describes general
industry knowledge — shipping terminology, regulatory definitions, standard
calculation formulas, public service descriptions. None of it identifies a
specific customer, contains a negotiated rate, or includes any personal or
financial data. This makes routing all FAQ traffic through the Claude API
both safe and appropriate, with no anonymization step required.

This classification is declared explicitly in code at the start of
`FAQService`'s processing flow, even though no enforcement action is
currently triggered by it. This is intentional: it establishes the audit
pattern this codebase will follow as later modules introduce data that
genuinely requires Tier 2 or Tier 3 handling, rather than retrofitting
classification logic after the fact.

## Trust vs. Sensitivity: A Note on Untrusted Input

The tier system above classifies data *sensitivity* — what the data is and
where it may be processed. It does not classify data *trust* — whether the
data's content could be attacker-controlled and attempt to manipulate
system behavior (prompt injection). These are orthogonal properties. A
Tier 1 user query is still untrusted input; a Tier 2 or 3 payload could in
principle be trusted or untrusted independently of its sensitivity.

This module's only untrusted input is the user's free-text query, handled
per `architecture.md`'s "Input Handling" section. As later modules
introduce external API responses (carrier data, tracking webhooks) as
inputs to agentic reasoning, those payloads will need to be evaluated on
*both* axes independently — e.g. a tracking webhook may be Tier 2
sensitive *and* untrusted, requiring both redaction and injection
resistance, not just one or the other.

## What Would Change This Classification

If a future enhancement to this module allowed an authenticated user to
ask "what's the status of *my* shipment" or referenced their specific
account, that query would immediately become Tier 2 — it now refers to
identifiable business data. This module currently has no such feature; it
answers only general, anonymous questions.

## Forward Look: Tier 2/3 in Later Phases

- **Quotation module**: client-specific negotiated rates are Tier 2.
  Presidio-based redaction is reserved for this phase, not implemented now.
- **Order Tracking module**: a specific tracking number tied to a customer
  account is Tier 2.
- **Booking module**: payment information is Tier 3 and will never be
  routed to the Claude API under any circumstance; any AI assistance
  touching payment data will run locally via Ollama/Llama 3.1.

## Secrets Handling

The Claude API key is stored exclusively in environment variables
(`ANTHROPIC_API_KEY`) and is never committed to source control.
`appsettings.Development.json` is excluded via `.gitignore`. GitLeaks is
run before every commit to catch any accidental secret exposure before it
reaches the remote repository.