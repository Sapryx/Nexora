using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IfSpacingAnalyzer : StyleAnalyzer
{
    public IfSpacingAnalyzer()
        : base(
            "NX0005",
            "Blank line before if",
            "An if statement that is not the first in its block must be preceded by a blank line"
        )
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var statement = (IfStatementSyntax)context.Node;
        var statements = statement.Parent switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax section => section.Statements,
            _ => default
        };

        if(statements.IndexOf(statement) > 0 && SyntaxLines.BlankLinesBefore(statement.GetFirstToken()) == 0)
        {
            Report(context, statement.IfKeyword.GetLocation());
        }
    }
}
