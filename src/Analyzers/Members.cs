using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzers;

public static class Members
{
    public static SyntaxToken Identifier(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
            PropertyDeclarationSyntax property => property.Identifier,
            IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
            EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier,
            ConstructorDeclarationSyntax constructor => constructor.Identifier,
            MethodDeclarationSyntax method => method.Identifier,
            BaseTypeDeclarationSyntax type => type.Identifier,
            _ => member.GetFirstToken()
        };
    }
}
