# ADR 0002: Keep persistence behind an application port

- **Status:** Accepted
- **Date:** 2026-11-18

## Context

The order creation use case needs to persist an aggregate. The domain and application layers should remain independent of EF Core and SQL Server so that business behavior can be tested without infrastructure.

## Decision

Define an IOrderRepository port in the Application layer. Implement it in Infrastructure using EF Core. The API composition root registers the infrastructure implementation.

## Consequences

- Application tests can use a lightweight in-memory test double.
- EF Core-specific configuration stays in Infrastructure.
- Repository methods expose asynchronous operations and cancellation.
- The initial implementation commits the order immediately. Transactional outbox behavior will be introduced alongside the first integration event, rather than adding unused infrastructure now.
