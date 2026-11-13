using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LocalFunctionAnalyzer : StyleAnalyzer
{
    public LocalFunctionAnalyzer()
        : base("NX0007", "Local function", "Local functions are not allowed; use a lambda or a method")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(it => Report(it, it.Node.GetLocation()), SyntaxKind.LocalFunctionStatement);
    }
}
