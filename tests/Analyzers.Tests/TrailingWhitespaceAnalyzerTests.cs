namespace Analyzers.Tests;

public class TrailingWhitespaceAnalyzerTests
{
    [Fact]
    public void Analyze_LineEndingWithSpaceOrTab_ReportsLine()
    {
        string source = "class A \n{\n    int a;\t\n}\n";

        Assert.Equal([1, 3], AnalyzerRunner.ReportedLines(new TrailingWhitespaceAnalyzer(), source));
    }

    [Fact]
    public void Analyze_CleanLines_ReportsNothing()
    {
        string source = "class A\n{\n\n    int a;\n}\n";

        Assert.Empty(AnalyzerRunner.ReportedLines(new TrailingWhitespaceAnalyzer(), source));
    }
}
