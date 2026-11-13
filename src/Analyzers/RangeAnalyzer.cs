using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RangeAnalyzer : StyleAnalyzer
{
    public RangeAnalyzer()
        : base("NX0009", "Range syntax", "Range and spread syntax with two dots is not allowed")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            it => Report(it, it.Node.GetLocation()),
            SyntaxKind.RangeExpression,
            SyntaxKind.SpreadElement,
            SyntaxKind.SlicePattern
        );
    }
}
