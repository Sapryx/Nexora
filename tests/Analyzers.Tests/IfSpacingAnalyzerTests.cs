namespace Analyzers.Tests;

public class IfSpacingAnalyzerTests
{
    [Fact]
    public void Analyze_IfWithoutBlankLineAfterAnotherStatement_ReportsIt()
    {
        string source = """
            class A
            {
                void M(int x)
                {
                    x++;
                    if(x > 1)
                    {
                    }
                }
            }
            """;

        Assert.Equal([6], AnalyzerRunner.ReportedLines(new IfSpacingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_FirstInBlockOrAfterBlankLineOrElseIf_ReportsNothing()
    {
        string source = """
            class A
            {
                void M(int x)
                {
                    if(x > 1)
                    {
                    }
                    else if(x > 0)
                    {
                    }

                    // comment
                    if(x > 2)
                    {
                    }

                    switch(x)
                    {
                        case 1:
                            if(x > 0)
                            {
                            }
                            break;
                    }
                }
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new IfSpacingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_CommentDirectlyAfterStatement_ReportsIf()
    {
        string source = """
            class A
            {
                void M(int x)
                {
                    x++;
                    // comment
                    if(x > 1)
                    {
                    }
                }
            }
            """;

        Assert.Equal([7], AnalyzerRunner.ReportedLines(new IfSpacingAnalyzer(), source));
    }
}
