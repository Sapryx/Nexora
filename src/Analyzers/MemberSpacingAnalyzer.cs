using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MemberSpacingAnalyzer : StyleAnalyzer
{
    public MemberSpacingAnalyzer()
        : base("NX0004", "Blank lines between members", "'{0}' must be preceded by {1} blank line(s)")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, TypeDeclarations.Kinds);
    }

    private static MemberGroup Group(MemberDeclarationSyntax member)
    {
        if(SyntaxLines.Start(member) != SyntaxLines.End(member) || HasComment(member))
        {
            return MemberGroup.None;
        }

        return member switch
        {
            FieldDeclarationSyntax or PropertyDeclarationSyntax => MemberGroup.Data,
            EventFieldDeclarationSyntax => MemberGroup.Event,
            MethodDeclarationSyntax method when method.Body == null && method.ExpressionBody == null =>
                MemberGroup.Signature,
            _ => MemberGroup.None
        };
    }

    private static bool HasComment(MemberDeclarationSyntax member)
    {
        return member.GetLeadingTrivia().Any(
            it => !it.IsKind(SyntaxKind.WhitespaceTrivia) &&
                  !it.IsKind(SyntaxKind.EndOfLineTrivia) &&
                  !it.IsDirective &&
                  !it.IsKind(SyntaxKind.DisabledTextTrivia)
        );
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var members = ((TypeDeclarationSyntax)context.Node).Members;

        for(int i = 1; i < members.Count; i++)
        {
            var group = Group(members[i]);
            int expected = group != MemberGroup.None && group == Group(members[i - 1]) ? 0 : 1;
            var firstToken = members[i].GetFirstToken();

            if(SyntaxLines.BlankLinesBefore(firstToken) != expected)
            {
                Report(context, firstToken.GetLocation(), Members.Identifier(members[i]).ValueText, expected);
            }
        }
    }
}
