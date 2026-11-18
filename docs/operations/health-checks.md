# API health checks

The API exposes separate liveness and readiness probes:

- `GET /health/live` reports whether the API process can respond. It does not probe dependencies.
- `GET /health/ready` checks dependencies required by the API to serve requests. It currently verifies SQL Server connectivity.
- `GET /health` remains available as a backwards-compatible alias for readiness.

All health endpoints are anonymous so orchestrators and load balancers can probe them without an Entra access token.

A readiness failure should remove the API instance from service; a liveness failure may trigger process restart. RabbitMQ is intentionally not part of API readiness: API requests persist outbox messages in SQL Server, while the worker owns broker connectivity and delivery.
