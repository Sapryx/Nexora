using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LineLengthAnalyzer : StyleAnalyzer
{
    private const int MaxLength = 120;

    public LineLengthAnalyzer()
        : base("NX0001", "Line too long", "Line is {0} characters long; the limit is 120")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxTreeAction(Analyze);
    }

    private void Analyze(SyntaxTreeAnalysisContext context)
    {
        foreach(var line in context.Tree.GetText(context.CancellationToken).Lines)
        {
            if(line.Span.Length > MaxLength)
            {
                Report(context, Location.Create(context.Tree, line.Span), line.Span.Length);
            }
        }
    }
}
