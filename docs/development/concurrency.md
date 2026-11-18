# Order concurrency

Orders use SQL Server `rowversion` as an EF Core optimistic concurrency token.

When an order is loaded for a lifecycle update, EF Core retains the rowversion read with that entity. The update statement includes that value in its predicate. If another transaction has changed the row first, SQL Server updates zero rows and EF Core raises `DbUpdateConcurrencyException`.

The Infrastructure repository translates that provider-specific exception into the Application-owned `OrderConcurrencyException`. The API exception handler returns HTTP 409 Problem Details so clients can reload the order and decide whether to retry.

This protects the read-modify-write interval from lost updates. It does not replace domain lifecycle validation: every transition still enforces its own allowed source status.

## Migration

Apply the `AddOrderRowVersion` migration using the documented EF Core migration workflow. The API does not apply migrations automatically at startup.
