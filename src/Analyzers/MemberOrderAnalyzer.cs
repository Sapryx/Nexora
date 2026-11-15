using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MemberOrderAnalyzer : StyleAnalyzer
{
    public MemberOrderAnalyzer()
        : base(
            "NX0003",
            "Member order",
            "'{0}' is out of order: properties, fields, events, constructors, then methods"
        )
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, TypeDeclarations.Kinds);
    }

    private static int Rank(MemberDeclarationSyntax member)
    {
        return member.Kind() switch
        {
            SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration => 0,
            SyntaxKind.FieldDeclaration => IsStyledProperty((FieldDeclarationSyntax)member) ? -1 : 1,
            SyntaxKind.EventFieldDeclaration or SyntaxKind.EventDeclaration => 2,
            SyntaxKind.ConstructorDeclaration or SyntaxKind.DestructorDeclaration => 3,
            SyntaxKind.MethodDeclaration
                or SyntaxKind.OperatorDeclaration
                or SyntaxKind.ConversionOperatorDeclaration => 4,
            _ => -1
        };
    }

    private static bool IsStyledProperty(FieldDeclarationSyntax field)
    {
        return field.Declaration.Type is GenericNameSyntax generic && generic.Identifier.ValueText == "StyledProperty";
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        int highestRank = 0;

        foreach(var member in ((TypeDeclarationSyntax)context.Node).Members)
        {
            int rank = Rank(member);

            if(rank >= 0 && rank < highestRank)
            {
                var identifier = Members.Identifier(member);
                Report(context, identifier.GetLocation(), identifier.ValueText);
            }

            highestRank = Math.Max(highestRank, rank);
        }
    }
}
