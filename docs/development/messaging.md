# Transactional outbox and order processing worker

When an order transitions into `Processing`, the same EF Core `SaveChangesAsync` call writes the order update and an `OutboxMessages` row in one SQL Server transaction. The worker claims due rows using SQL Server `UPDLOCK`, `READPAST` and a time-limited lease, publishes persistent messages to RabbitMQ with publisher confirms, then marks them published.

A crash after broker acceptance but before the outbox row is marked published can cause redelivery after the lease expires. This is intentional at-least-once delivery. The consumer is idempotent: an order already in `Fulfilled` is treated as successfully processed.

Transient consumer failures are sent to a 30-second TTL retry queue, which dead-letters back to the processing queue. After five deliveries, the message is routed to `orders.processing.dead`. Outbox publishing failures use exponential backoff (5 seconds doubling to a 5-minute cap) and are marked dead-lettered in SQL after ten attempts.

## Local setup

Set `MSSQL_SA_PASSWORD` and `RABBITMQ_DEFAULT_PASS` in `.env`, then start dependencies and worker:

```bash
docker compose up -d sqlserver rabbitmq
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api
docker compose up -d --build worker
```

The RabbitMQ management UI is available at http://localhost:15672. The worker expects the database migrations to have been applied before it starts. Configure `ConnectionStrings__Orders` for a local worker process.
