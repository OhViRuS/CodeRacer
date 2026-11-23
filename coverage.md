# Code coverage

Run the complete unit and integration test suite with coverage:

```powershell
.\scripts\coverage.ps1
```

The script restores the pinned ReportGenerator tool, runs tests against the PostgreSQL Testcontainer, enforces the 50% total line-coverage threshold, and creates the HTML report at:

```text
coverage-report/index.html
```

Docker must be running for this coverage script because it explicitly requires the real PostgreSQL Testcontainer. If Docker is unavailable during a normal local `dotnet test`, the integration factory can use an EF Core InMemory database and prints a warning. That fallback is only for local development; it must not be used to claim relational integration coverage.

Generated test results and reports are not source files and should not be committed.

For a normal local test run:

```powershell
dotnet test CodeRacer.Server.Tests/CodeRacer.Server.Tests.csproj
```

If Docker is not available, integration tests use a temporary EF Core InMemory database and print a warning. To make a normal test run fail instead of using the fallback, run:

```powershell
$env:CODE_RACER_REQUIRE_POSTGRES = 'true'
dotnet test CodeRacer.Server.Tests/CodeRacer.Server.Tests.csproj
```
