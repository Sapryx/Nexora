namespace Analyzers.Tests;

public class LineLengthAnalyzerTests
{
    [Fact]
    public void Analyze_LineLongerThanLimit_ReportsLine()
    {
        string source = "class A\n{\n    string s = \"" + new string('a', 110) + "\";\n}\n";

        Assert.Equal([3], AnalyzerRunner.ReportedLines(new LineLengthAnalyzer(), source));
    }

    [Fact]
    public void Analyze_LineAtLimit_ReportsNothing()
    {
        string line = "// " + new string('a', 117);
        string source = "class A\n{\n}\n" + line + "\n";

        Assert.Empty(AnalyzerRunner.ReportedLines(new LineLengthAnalyzer(), source));
    }
}
