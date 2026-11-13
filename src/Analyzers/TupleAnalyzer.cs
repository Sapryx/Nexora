using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TupleAnalyzer : StyleAnalyzer
{
    public TupleAnalyzer()
        : base("NX0008", "Tuple", "Tuples are not allowed; declare a small class with named properties")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            it => Report(it, it.Node.GetLocation()),
            SyntaxKind.TupleType,
            SyntaxKind.TupleExpression,
            SyntaxKind.ParenthesizedVariableDesignation
        );
    }
}
