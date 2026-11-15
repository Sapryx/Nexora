namespace Analyzers.Tests;

public class TupleAnalyzerTests
{
    [Fact]
    public void Analyze_TupleTypesLiteralsAndDeconstruction_ReportsThem()
    {
        string source = """
            using System.Collections.Generic;

            class A
            {
                (int, string) M(Dictionary<int, string> dictionary)
                {
                    var (a, b) = (1, "b");
                    foreach(var (key, value) in dictionary)
                    {
                    }
                    int c = (a + 1) * 2;
                    return default;
                }
            }
            """;

        Assert.Equal([5, 7, 7, 8], AnalyzerRunner.ReportedLines(new TupleAnalyzer(), source));
    }
}
