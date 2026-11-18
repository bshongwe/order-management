# ADR 0001: Start with a modular monolith

- **Status:** Accepted
- **Date:** 2026-11-18

## Context

The assessment requires an API, persistence, asynchronous messaging and a web client within a constrained delivery window. Starting with distributed services would introduce deployment, networking, observability and data-consistency costs before independent scaling or team ownership has been demonstrated as necessary.

## Decision

Build a modular monolith with explicit domain and application boundaries. Run background message processing as a separately hosted worker process when needed, while keeping it in the same solution and deployment unit initially.

## Consequences

- Simpler local development, transactions and deployment.
- Clear module boundaries remain enforceable and testable.
- A module can be extracted later if operational or organizational evidence supports the cost.
- Shared database access must not become permission for modules to bypass one another's contracts.
