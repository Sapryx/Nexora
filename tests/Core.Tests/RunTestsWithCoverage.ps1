$ErrorActionPreference = "Stop"

$testResultsDir = Join-Path $PSScriptRoot "Test Results"
$reportDir = Join-Path $PSScriptRoot "Report"
$coverageGlob = Join-Path $PSScriptRoot "Test Results/**/coverage.cobertura.xml"

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $testResultsDir

dotnet test --collect:"XPlat Code Coverage" --results-directory:"$testResultsDir"
reportgenerator -reports:"$coverageGlob" -targetdir:"$reportDir" -reporttypes:Html
