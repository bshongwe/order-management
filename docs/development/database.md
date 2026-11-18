# Local SQL Server

The repository uses SQL Server through Entity Framework Core. The database is intentionally
not started or migrated automatically by the API.

## Start the database

1. Copy `.env.example` to `.env` and change the development password.
2. Start SQL Server:

   ```bash
   docker compose up -d sqlserver
   docker compose ps
   ```

3. Configure the API connection string in the shell that launches the API:

   **Bash**
   ```bash
   export ConnectionStrings__Orders='Server=localhost,1433;Database=OrderManagement;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=False'
   dotnet run --project src/OrderManagement.Api
   ```

   **PowerShell**
   ```powershell
   $env:ConnectionStrings__Orders = 'Server=localhost,1433;Database=OrderManagement;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=False'
   dotnet run --project src/OrderManagement.Api
   ```

The `ConnectionStrings__Orders` environment variable overrides configuration and should
be used for local secrets and deployment configuration. Do not commit real credentials.

## EF Core tooling and migrations

The repository pins the EF Core CLI to the same 8.0.0 version as the EF Core packages.
Restore the local tool manifest once after cloning or pulling:

```bash
dotnet tool restore
```

EF Core tooling uses `OrdersDbContextFactory` and reads the connection string from
`ConnectionStrings__Orders`. Set that variable in the current shell before running commands.
The factory validates configuration but does not connect to SQL Server when scaffolding a migration.

Create a migration from the repository root:

```bash
dotnet ef migrations add InitialCreate \
  --project src/OrderManagement.Infrastructure \
  --startup-project src/OrderManagement.Api \
  --output-dir Persistence/Migrations
```

Review and commit all generated migration and model snapshot files. Do not edit generated
migration metadata by hand. To inspect the migrations known to the project:

```bash
dotnet ef migrations list \
  --project src/OrderManagement.Infrastructure \
  --startup-project src/OrderManagement.Api
```

Apply migrations explicitly to the configured database:

```bash
dotnet ef database update \
  --project src/OrderManagement.Infrastructure \
  --startup-project src/OrderManagement.Api
```

For production, run migrations as a controlled deployment step using the deployment
environment's secret store and an appropriately scoped database identity. Do not run
`Database.Migrate()` automatically from API startup: multiple API replicas can race,
and application startup should not require schema-changing privileges.

## Stop the database

```bash
docker compose down
```

To also delete the local SQL Server data volume (destructive):

```bash
docker compose down --volumes
```

Database schema migrations are applied as an explicit deployment/development step; the API
does not run migrations on startup.
