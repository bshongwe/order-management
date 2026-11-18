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
