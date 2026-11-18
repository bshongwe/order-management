# Assessment delivery checklist

- [x] .NET solution with Domain, Application, Infrastructure, API and Worker projects
- [x] SQL Server and EF Core code-first migrations
- [x] Customer and order workflows with server-calculated totals
- [x] SADC country/currency validation
- [x] Order lifecycle transitions and optimistic concurrency
- [x] Idempotent lifecycle commands
- [x] Transactional outbox, RabbitMQ publisher/consumer, retry and dead-letter handling
- [x] Microsoft Entra JWT authentication and authorization policies
- [x] React + TypeScript customer/order UI
- [x] Docker Compose stack and demo seed data
- [x] Unit and SQL Server Testcontainers integration tests
- [x] CI Release build, automated tests and frontend production build
- [x] API documentation, correlation IDs, request metrics and order ETags
- [x] README and technical/SQL assessment answers

## Deliberate production follow-ups

- Interactive Entra sign-in in the browser (the current operator UI accepts an access token in memory)
- OpenTelemetry exporter, dashboards and alerting
- Full browser E2E tests and production deployment smoke tests
- Managed secrets provider, backup/restore drills and a controlled production migration job
- Conditional write requests using If-Match if required

These are explicitly identified follow-ups, not capabilities claimed as complete.
