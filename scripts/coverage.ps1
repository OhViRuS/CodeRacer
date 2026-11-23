$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$testResults = Join-Path $root 'TestResults'
$coverageReport = Join-Path $root 'coverage-report'

Remove-Item $testResults -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $coverageReport -Recurse -Force -ErrorAction SilentlyContinue

Push-Location $root
try {
	dotnet tool restore
	if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

	dotnet test CodeRacer.Server.Tests/CodeRacer.Server.Tests.csproj `
		--settings coverage.runsettings `
		--collect:"XPlat Code Coverage" `
		--results-directory TestResults
	if ($LASTEXITCODE -ne 0) { throw "Tests or coverage threshold failed." }

	$coverageFiles = Get-ChildItem TestResults -Recurse -Filter coverage.cobertura.xml |
		ForEach-Object { $_.FullName }

	if (-not $coverageFiles) {
		throw "No coverage file was generated."
	}

	dotnet tool run reportgenerator `
		"-reports:$($coverageFiles -join ';')" `
		"-targetdir:$coverageReport" `
		'-reporttypes:Html;Cobertura'
	if ($LASTEXITCODE -ne 0) { throw "ReportGenerator failed." }

	Write-Host "Coverage report: $(Join-Path $coverageReport 'index.html')"
}
finally {
	Pop-Location
}
