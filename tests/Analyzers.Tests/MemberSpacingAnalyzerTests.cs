namespace Analyzers.Tests;

public class MemberSpacingAnalyzerTests
{
    [Fact]
    public void Analyze_ProperlySpacedMembers_ReportsNothing()
    {
        string source = """
            class A
            {
                public int Size { get; set; }
                private const int Limit = 1;
                private int count;
            #if !DEBUG
                private int debugCount;
            #endif

                [Obsolete]
                public int Old { get; set; }

                public int Total
                {
                    get => count;
                }

                private int[] items = [
                    1
                ];

                public event System.Action? Changed;
                public event System.Action? Closed;

                public A()
                {
                }

                // comment
                void M()
                {
                }
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new MemberSpacingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_ContiguousMethodSignatures_ReportsNothing()
    {
        string source = """
            interface I
            {
                int Size { get; }

                void Play();
                void Stop();
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new MemberSpacingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_CommentedMembersSeparatedByBlankLines_ReportsNothing()
    {
        string source = """
            interface I
            {
                int Size { get; }

                /// <summary>Title.</summary>
                string Title { get; }

                void Play();

                // Stops playback.
                void Stop();
            }
            """;

        Assert.Empty(AnalyzerRunner.ReportedLines(new MemberSpacingAnalyzer(), source));
    }

    [Fact]
    public void Analyze_WrongBlankLines_ReportsMembers()
    {
        string source = """
            class A
            {
                public int Size { get; set; }

                private int count;
                [Obsolete]
                public int Old { get; set; }
                public event System.Action? Changed;


                public A()
                {
                }
                void M()
                {
                }
            }
            """;

        Assert.Equal([5, 6, 8, 11, 14], AnalyzerRunner.ReportedLines(new MemberSpacingAnalyzer(), source));
    }
}
