namespace Analyzers.Tests;

public class NullCheckAnalyzerTests
{
    [Fact]
    public void Analyze_IsNullAndIsNotNull_ReportsThem()
    {
        string source = """
            class A
            {
                bool M(object? x)
                {
                    bool a = x is null;
                    bool b = x is not null;
                    bool c = x == null || x != null;
                    bool d = x is string;
                    bool e = x is not string;
                    return a && b && c && d && e;
                }
            }
            """;

        Assert.Equal([5, 6], AnalyzerRunner.ReportedLines(new NullCheckAnalyzer(), source));
    }
}
