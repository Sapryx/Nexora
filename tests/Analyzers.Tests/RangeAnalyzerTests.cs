namespace Analyzers.Tests;

public class RangeAnalyzerTests
{
    [Fact]
    public void Analyze_RangesSpreadsAndSlices_ReportsThem()
    {
        string source = """
            class A
            {
                void M(string text, int[] items)
                {
                    string a = text[..2];
                    int[] b = items[1..];
                    int[] c = [.. items];
                    bool d = items is [1, ..];
                    int[] e = [1, 2];
                }
            }
            """;

        Assert.Equal([5, 6, 7, 8], AnalyzerRunner.ReportedLines(new RangeAnalyzer(), source));
    }
}
