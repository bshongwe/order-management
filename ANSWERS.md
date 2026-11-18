# Technical assessment answers

## Architecture and design

### 1. Why a modular monolith instead of microservices?
The domain is cohesive and the assessment does not justify the operational cost of independently deployed services. A modular monolith keeps domain, application, infrastructure and API responsibilities explicit while allowing future extraction at stable boundaries. The worker is a separate process because asynchronous consumption has a distinct lifecycle and scaling profile.

### 2. What does each layer own?
- Domain: entities, lifecycle invariants and business rules; no infrastructure dependencies.
- Application: use cases, commands/queries and repository abstractions.
- Infrastructure: EF Core, SQL Server, RabbitMQ and persistence implementations.
- API: HTTP contracts, validation, authentication, authorization and dependency composition.
- Worker: outbox publishing and asynchronous fulfilment consumption.

### 3. How are totals calculated?
The API accepts SKU, quantity and unit price. Domain line items validate their values; the aggregate calculates totals using checked decimal arithmetic. Client-supplied totals are never trusted. SQL stores money as decimal(18,2).

### 4. How is the order lifecycle protected?
Explicit domain methods enforce allowed transitions. Application handlers translate missing/conflict outcomes to HTTP 404/409. Callers cannot set arbitrary status strings.

### 5. How are SADC country/currency combinations handled?
Country and currency codes are normalized and checked against the supported policy. CMA currencies are permitted where applicable. This is validation, not FX conversion; no implicit exchange rate is applied.

## Persistence and SQL

### 6. Why SQL Server and EF Core code-first?
SQL Server provides transactions, relational constraints, indexes and rowversion concurrency. EF Core code-first keeps schema changes versioned through reviewed migrations while targeted SQL remains available for concurrency-sensitive worker operations.

### 7. What indexes matter?
The schema includes unique customer email, customer country, order customer/status/creation time, customer reference, line-item order ID, idempotency key and outbox due/published/dead-letter indexes. Indexes support actual lookup, paging, uniqueness and worker claim patterns; further indexes should be justified by query plans.

### 8. How does optimistic concurrency work?
Orders use SQL Server rowversion. EF includes the original token in update predicates; if another transaction changed the row, EF raises DbUpdateConcurrencyException. The API maps this to HTTP 409 instead of overwriting newer state.

### 9. Why use a transactional outbox?
The order transition into Processing and its order.processing.requested outbox record are saved in one SQL transaction. This avoids the dual-write failure where database commit and broker publication diverge. A worker publishes committed outbox messages.

### 10. Is delivery exactly once?
No. The outbox and RabbitMQ provide at-least-once delivery. A crash after broker confirmation but before marking the outbox row published can cause redelivery. Consumers must be idempotent; exactly-once side effects across independent systems are not claimed.

### 11. How are database changes deployed?
EF migrations are committed with the code that needs them. Compose runs a migrator before API/worker startup. Production should use a reviewed idempotent SQL script, a dedicated migration identity, backups and expand-contract changes for zero-downtime compatibility. Application instances should not race to migrate schemas.

## API and security

### 12. How is the API secured?
Microsoft Entra JWT bearer validation checks authority and audience. A fallback policy requires authentication. Orders.Read, Orders.Write and Orders.Admin authorize operations through delegated scopes or application roles. Health endpoints and the service root are anonymous; business endpoints are protected.

### 13. How is request validation performed?
The API validates transport shape and required fields at the boundary. Domain constructors/methods enforce invariants again so invalid state cannot enter through another caller. Validation uses field-specific problem details.

### 14. Why use idempotency keys?
Clients may retry after timeouts without knowing whether the first request committed. Lifecycle commands require Idempotency-Key; persistence stores the key, operation fingerprint and serialized response under a unique constraint. Replays return the original result.

### 15. How is pagination protected?
List endpoints use bounded page sizes (maximum 100), deterministic ordering and server-side paging/count queries. This avoids unbounded result sets and stabilizes ordering for a fixed dataset.

### 16. What are ETags used for?
Order detail responses expose an ETag derived from SQL Server rowversion. Clients use If-None-Match to avoid downloading unchanged representations. Conditional write protection is provided by EF concurrency and HTTP 409; this API does not claim If-Match write semantics.

## Messaging and operations

### 17. How are retries and poison messages handled?
Outbox publishing uses bounded exponential backoff and dead-letters after its attempt limit. RabbitMQ consumer failures use a delayed retry queue and eventually route to a dead-letter queue. Operators inspect dead-lettered rows/messages, correct the cause and replay through a controlled procedure.

### 18. What are health checks and correlation IDs for?
Liveness indicates the process can respond. Readiness includes SQL Server and should gate traffic. A validated or generated correlation ID is returned in X-Correlation-ID and attached to logging/activity context. Request counters and duration histograms provide low-cardinality measurements.

### 19. How should secrets be managed?
Passwords, Entra values and connection strings come from environment-specific secret stores. .env is local-only and excluded from Docker build contexts. Never commit real credentials, tokens or production connection strings.

### 20. What would you improve next for production?
Add interactive Entra sign-in (for example MSAL), OpenTelemetry export, dashboards/alerts, browser E2E tests, backup/restore exercises, rate limiting, CSP/security headers, secret rotation and deployment smoke tests. Validate indexes and query plans against representative workloads. Add If-Match if clients need conditional writes.
