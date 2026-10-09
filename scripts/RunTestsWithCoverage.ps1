$ErrorActionPreference = "Stop"

$testProjectDir = Join-Path $PSScriptRoot "../tests/Core.Tests"
$testResultsDir = Join-Path $testProjectDir "Test Results"
$reportDir = Join-Path $testProjectDir "Report"
$coverageGlob = Join-Path $testProjectDir "Test Results/**/coverage.cobertura.xml"

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $testResultsDir

dotnet test $testProjectDir --collect:"XPlat Code Coverage" --results-directory:"$testResultsDir"
reportgenerator -reports:"$coverageGlob" -targetdir:"$reportDir" -reporttypes:Html
