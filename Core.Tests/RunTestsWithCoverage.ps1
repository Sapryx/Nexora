Remove-Item -Recurse -Force -ErrorAction SilentlyContinue Core.Tests/TestResults

dotnet test --collect:"XPlat Code Coverage" --results-directory:"Core.Tests/TestResults"
reportgenerator -reports:"Core.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"Core.Tests/Report" -reporttypes:Html