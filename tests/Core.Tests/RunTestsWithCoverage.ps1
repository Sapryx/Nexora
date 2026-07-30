Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "Core.Tests/Test Results"

dotnet test --collect:"XPlat Code Coverage" --results-directory:"Core.Tests/Test Results"
reportgenerator -reports:"Core.Tests/Test Results/**/coverage.cobertura.xml" -targetdir:"Core.Tests/Report" -reporttypes:Html