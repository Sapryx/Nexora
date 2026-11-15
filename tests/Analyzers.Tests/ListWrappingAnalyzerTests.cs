namespace Analyzers.Tests;

public class ListWrappingAnalyzerTests
{
    [Fact]
    public void Analyze_ProperlyWrappedLists_ReportsNothing()
    {
        string source = """
            class A
            {
                int[] items = [
                    1,
                    2
                ];
                int[] inline = [1, 2];

                A(
                    int a,
                    int b)
                {
                }

                void M(int a, int b)
                {
                    M(
                        a,
                        Twice(
                            b
                        )
                    );
                    Run(a, () =>
                    {
                    });
                    int[] values = [
                        1,
                        2
                    ];
                }
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new ListWrappingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_BadlyWrappedLists_ReportsThem()
    {
        string source = """
            class A
            {
                A(int a,
                    int b)
                {
                }

                A(
                    int a, int b)
                {
                }

                A(
                    int a,
                    int b
                )
                {
                }

                void M(int a, int b)
                {
                    M(a,
                        b);
                    M(
                        a,
                        b);
                    int[] values =
                        [1,
                        2];
                }
            }
            """;

        Assert.Equal([3, 8, 13, 22, 24, 28], AnalyzerRunner.ReportedLines(new ListWrappingAnalyzer(), source));
    }
}
