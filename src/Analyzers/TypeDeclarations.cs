using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzers;

public static class TypeDeclarations
{
    public static readonly ImmutableArray<SyntaxKind> Kinds = [
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.InterfaceDeclaration
    ];
}
