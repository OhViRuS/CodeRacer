# Code coverage

Run the complete unit and integration test suite with coverage:

```powershell
.\scripts\coverage.ps1
```

The script restores the pinned ReportGenerator tool, runs tests against the PostgreSQL Testcontainer, enforces the 50% total line-coverage threshold, and creates the HTML report at:

```text
coverage-report/index.html
```

Docker must be running because the integration tests use a real PostgreSQL Testcontainer. Generated test results and reports are not source files and should not be committed.
