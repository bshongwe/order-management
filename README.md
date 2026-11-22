# SADC Order Management

A full-stack order-management platform for Southern African Development Community markets, built with .NET 8, SQL Server/EF Core, React/TypeScript, Microsoft Entra ID and RabbitMQ.

## Solution structure

- Domain: customer/order entities and lifecycle invariants.
- Application: use cases, queries and repository contracts.
- Infrastructure: EF Core, SQL Server, idempotency and transactional outbox.
- API: REST endpoints, validation, Entra authorization, health and OpenAPI.
- Worker: outbox publisher and RabbitMQ fulfilment consumer.
- web: React + TypeScript operations UI.
- tests: unit tests and SQL Server Testcontainers integration tests.

## Prerequisites

.NET SDK 8, Docker with Compose, and (for local frontend development) Node.js 22+. Protected API calls require a Microsoft Entra app registration.

## Run the complete stack

Copy .env.example to .env and replace the local passwords and Entra values. Set ENTRA_AUTHORITY to your tenant-specific v2.0 authority and ENTRA_AUDIENCE to the API audience.

```bash
cp .env.example .env
docker compose up --build
```

- Web UI: http://localhost:8082
- API: http://localhost:8090
- Swagger UI: http://localhost:8090/swagger/index.html
- OpenAPI document: http://localhost:8090/swagger/v1/swagger.json
- RabbitMQ management: http://localhost:15672

The Compose API runs in Production mode and explicitly sets `OpenApi__Enabled=true` to expose Swagger for the local assessment stack. Its published port is restricted to the host loopback address (`127.0.0.1`) in Docker Compose. When running the API directly in Development, Swagger is enabled automatically. For other Production deployments, set `OpenApi__Enabled=true` only when the documentation endpoint should be exposed; otherwise leave it unset or false. API operations remain protected by the configured Entra authorization policies.

The Compose migrator waits for SQL Server health and applies EF migrations before API/worker startup. The API has no authentication bypass.

### Demo data

Run the development-only seed script against the OrderManagement database:

```bash
sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d OrderManagement -i scripts/seed-demo-data.sql
```

It inserts 50 demo customers and 1,000 draft orders. Use only in a disposable development database.

## Run services locally

```bash
dotnet tool restore
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api
dotnet run --project src/OrderManagement.Api
dotnet run --project src/OrderManagement.Worker
```

Set ConnectionStrings__Orders, Entra__Authority and Entra__Audience. For Vite, allow http://localhost:5173 in Cors:AllowedOrigins, then run:

```bash
cd web
npm install
npm run dev
```

The operator UI accepts an Entra API access token in memory for testing; it does not persist tokens to local storage or cookies. Grant Orders.Read and Orders.Write scopes or application roles as described in docs/security/entra-authentication.md.

## API overview

- Customers: create, retrieve and paginated/search list.
- Orders: create, retrieve and paginated list.
- Lifecycle: submit, confirm, process, fulfil and cancel.
- Lifecycle commands require Idempotency-Key.
- Protected endpoints require Entra JWT access tokens and Orders.Read/Orders.Write permissions.
- /health/live is process liveness; /health/ready checks SQL readiness. /health remains a readiness alias.
- X-Correlation-ID is accepted/generated and returned on API responses.
- Order detail responses expose an ETag based on SQL Server rowversion; If-None-Match returns 304 when unchanged.

## Lifecycle and reliability

Draft → Submitted → Confirmed → Processing → Fulfilled. Orders may be cancelled where domain rules permit. Totals are calculated by the server from validated line items.

When an order enters Processing, the order update and outbox event commit in one SQL transaction. The worker publishes persistent messages to durable RabbitMQ queues with publisher confirms. Delivery is at least once; the consumer is idempotent. Retries are bounded and poison messages are dead-lettered. See docs/development/messaging.md.

## Migrations

Migrations live in src/OrderManagement.Infrastructure/Persistence/Migrations. See docs/development/migrations.md for idempotent script generation, expand-contract deployment and rollback guidance.

## Tests and CI

```bash
dotnet restore OrderManagement.sln
dotnet build OrderManagement.sln --configuration Release --no-restore
dotnet test OrderManagement.sln --configuration Release --no-build --verbosity normal
cd web && npm install && npm run build
```

GitHub Actions builds/tests .NET and runs the React TypeScript production build. Integration tests provision a fresh SQL Server Testcontainer and apply the complete migration chain.

## Security and operations

Never commit .env, credentials, bearer tokens or production connection strings. Use managed secrets and a dedicated migration identity in production. Swagger is disabled by default in Production; expose it only where access is controlled. Metrics use low-cardinality method/status tags; export through OpenTelemetry for dashboards and alerts.

See ANSWERS.md and docs/assessment/definition-of-done.md for design rationale and assessment checklist.
