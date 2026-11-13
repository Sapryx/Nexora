namespace Analyzers.Tests;

public class LocalFunctionAnalyzerTests
{
    [Fact]
    public void Analyze_LocalFunction_ReportsIt()
    {
        string source = """
            class A
            {
                void M()
                {
                    int Twice(int x) => x * 2;
                    System.Func<int, int> lambda = it => it * 2;
                }
            }
            """;

        Assert.Equal([5], AnalyzerRunner.ReportedLines(new LocalFunctionAnalyzer(), source));
    }
}
