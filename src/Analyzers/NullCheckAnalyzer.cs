using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullCheckAnalyzer : StyleAnalyzer
{
    public NullCheckAnalyzer()
        : base("NX0006", "Null check with is", "Use == null or != null instead of is null or is not null")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IsPatternExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var expression = (IsPatternExpressionSyntax)context.Node;
        var pattern = expression.Pattern is UnaryPatternSyntax unary ? unary.Pattern : expression.Pattern;

        if(pattern is ConstantPatternSyntax constant && constant.Expression.IsKind(SyntaxKind.NullLiteralExpression))
        {
            Report(context, expression.GetLocation());
        }
    }
}
