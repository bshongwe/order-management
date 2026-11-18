# Integration testing

The integration test project verifies persistence behavior against the same database engine
used by the application: Microsoft SQL Server.

## Prerequisites

- Docker Engine or Docker Desktop must be running.
- The Docker daemon must be accessible to the current user.
- The first test run downloads the SQL Server container image.

No local SQL Server installation, application connection string, or running development
database is required. Testcontainers starts an isolated SQL Server container for the test
collection and removes it when the tests finish.

## Run the tests

Run all unit and integration tests from the repository root:

```bash
dotnet test OrderManagement.sln --configuration Release
```

Run only the SQL Server integration tests:

```bash
dotnet test tests/OrderManagement.IntegrationTests/OrderManagement.IntegrationTests.csproj --configuration Release
```

The fixture applies the checked-in EF Core migrations before tests execute. Each test clears
the `Orders` table before running, while the SQL Server container is shared by the test
collection to avoid repeatedly downloading and starting the database image.

The integration tests intentionally use `Database.MigrateAsync()` against a disposable test
database. This is test setup only; the API does not apply migrations automatically at startup.

## CI

The existing GitHub Actions workflow builds and tests the solution. Because the integration
test project is part of `OrderManagement.sln`, it is included automatically. GitHub-hosted
Ubuntu runners provide Docker for Testcontainers.
