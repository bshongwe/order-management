# HTTP idempotency

Order status transition endpoints require an `Idempotency-Key` request header (1–128 characters):

- `POST /api/orders/{id}/submit`
- `POST /api/orders/{id}/confirm`
- `POST /api/orders/{id}/process`
- `POST /api/orders/{id}/fulfill`
- `POST /api/orders/{id}/cancel`

The API persists the key, a request fingerprint (operation + order ID), and the serialized response in SQL Server. The order mutation and idempotency record are committed in the same database transaction. A retry with the same key and request fingerprint replays the original response and adds `Idempotency-Replayed: true`. Reusing a key for a different operation or order returns HTTP 409 Problem Details. Missing or oversized keys return HTTP 400.

The unique key index protects the store across API instances. Optimistic concurrency remains an independent safeguard for competing requests that use different keys. Idempotency records are retained until an explicit retention policy is introduced; production deployments should define a retention period that exceeds the maximum client retry window and safely purge older records.

Example:

```http
POST /api/orders/00000000-0000-0000-0000-000000000001/submit HTTP/1.1
Host: localhost:5000
Idempotency-Key: submit-order-0001
```

The current implementation stores successful and deterministic business outcomes (such as not-found and invalid-transition responses) so retries remain stable. Unexpected failures roll back both the order mutation and the idempotency record.
