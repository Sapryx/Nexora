namespace Analyzers.Tests;

public class MemberOrderAnalyzerTests
{
    [Fact]
    public void Analyze_MembersInOrder_ReportsNothing()
    {
        string source = """
            class A
            {
                public static readonly StyledProperty<int> SizeProperty = Register();

                public int Size { get; set; }
                public int this[int index] => index;
                private const int Limit = 1;
                private int count;

                public event System.Action? Changed;

                public A()
                {
                }

                void M()
                {
                }

                class Nested
                {
                }
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new MemberOrderAnalyzer(), source));
    }

    [Fact]
    public void Analyze_MemberAfterLaterKind_ReportsIt()
    {
        string source = """
            class A
            {
                private int count;
                public int Size { get; set; }

                void M()
                {
                }

                public A()
                {
                }

                public event System.Action? Changed;
            }
            """;

        Assert.Equal([4, 10, 14], AnalyzerRunner.ReportedLines(new MemberOrderAnalyzer(), source));
    }
}
