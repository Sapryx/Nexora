namespace Analyzers.Tests;

public class BuiltInTypeVarAnalyzerTests
{
    [Fact]
    public void Analyze_VarForBuiltInTypes_ReportsIt()
    {
        string source = """
            using System.Collections.Generic;

            class A
            {
                void M(Dictionary<string, int> map)
                {
                    var count = 0;
                    var text = "";
                    foreach(var item in new long[0])
                    {
                    }
                    map.TryGetValue("a", out var value);
                    var bytes = new byte[1];
                    var list = new List<int>();
                    int[] explicitArray = new int[0];
                    double ratio = 0.5;
                }
            }
            """;

        Assert.Equal([7, 8, 9, 12], AnalyzerRunner.ReportedLines(new BuiltInTypeVarAnalyzer(), source));
    }
}
