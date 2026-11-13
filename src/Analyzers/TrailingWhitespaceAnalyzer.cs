using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TrailingWhitespaceAnalyzer : StyleAnalyzer
{
    public TrailingWhitespaceAnalyzer()
        : base("NX0002", "Trailing whitespace", "Line ends with whitespace")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxTreeAction(Analyze);
    }

    private void Analyze(SyntaxTreeAnalysisContext context)
    {
        var text = context.Tree.GetText(context.CancellationToken);

        foreach(var line in text.Lines)
        {
            if(line.Span.Length > 0 && char.IsWhiteSpace(text[line.End - 1]))
            {
                Report(context, Location.Create(context.Tree, line.Span));
            }
        }
    }
}
