# Architecture

## Initial direction

The system will start as a **modular monolith** with independently runnable API and background-worker processes where operationally useful. This keeps deployment and local development straightforward while preserving clear boundaries for future extraction if evidence justifies it.

### Proposed logical modules

- **Orders** — order lifecycle, validation and order-facing use cases.
- **Reference Data** — supported SADC countries, currencies and related policy.
- **Messaging** — outbox publishing and message-consumer infrastructure.
- **Identity and Access** — authentication integration and authorization policies.

The exact project and assembly boundaries will be confirmed when the solution is scaffolded.

## Architectural constraints

1. Domain rules must not depend on infrastructure or web frameworks.
2. Application use cases coordinate domain behavior and persistence abstractions.
3. Infrastructure owns EF Core, SQL Server and RabbitMQ adapters.
4. API endpoints remain thin and expose stable HTTP contracts.
5. Cross-module interactions should use explicit contracts rather than reaching into another module's persistence implementation.
6. External side effects must not be coupled to an unprotected database transaction. Use a transactional outbox for reliable publication.
7. Consumers must be idempotent because broker delivery is at least once.

## Decisions

See the [Architecture Decision Records](decisions/).
