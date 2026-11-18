# Database migration and deployment strategy

## Development

Apply migrations with the repository-pinned EF CLI:

```bash
dotnet tool restore
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api
```

Generate a reviewed idempotent deployment script:

```bash
dotnet ef migrations script --idempotent --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api --output artifacts/order-management-migrations.sql
```

The generated SQL is a release artifact; do not edit it by hand. Integration tests start an empty SQL Server Testcontainer and apply all migrations, detecting broken migration chains against a clean database.

## Production rollout

1. Back up the database and verify restore procedures.
2. Generate the idempotent script from the exact release commit and review it with the application change.
3. Apply schema changes with a dedicated migration identity before deploying application instances.
4. Prefer expand-contract changes: add nullable/new structures first, deploy code compatible with both schemas, backfill in bounded batches, then enforce constraints/remove obsolete structures in a later release.
5. Deploy API and worker only after migration completion; run readiness and smoke checks.
6. Retain the previous application image and a tested database restore plan. A down migration is not assumed safe for data-destructive changes.

## Rollback

Roll back application binaries first only when the previous version remains compatible with the expanded schema. For destructive/data transformations, restore from backup or run a separately reviewed compensating migration. Never automatically run EF Down in production as a generic rollback.

## Compose

The local Compose migrator applies migrations before API and worker start. This is a developer convenience, not a substitute for a controlled production migration pipeline.
