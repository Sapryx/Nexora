using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BuiltInTypeVarAnalyzer : StyleAnalyzer
{
    private static readonly HashSet<SpecialType> BuiltInTypes = [
        SpecialType.System_Boolean,
        SpecialType.System_Byte,
        SpecialType.System_SByte,
        SpecialType.System_Char,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_IntPtr,
        SpecialType.System_UIntPtr,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_String
    ];

    public BuiltInTypeVarAnalyzer()
        : base("NX0011", "var for a built-in type", "Spell out the built-in type '{0}' instead of var")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            it => Analyze(it, ((VariableDeclarationSyntax)it.Node).Type),
            SyntaxKind.VariableDeclaration
        );
        context.RegisterSyntaxNodeAction(
            it => Analyze(it, ((ForEachStatementSyntax)it.Node).Type),
            SyntaxKind.ForEachStatement
        );
        context.RegisterSyntaxNodeAction(
            it => Analyze(it, ((DeclarationExpressionSyntax)it.Node).Type),
            SyntaxKind.DeclarationExpression
        );
    }

    private void Analyze(SyntaxNodeAnalysisContext context, TypeSyntax type)
    {
        if(!type.IsVar)
        {
            return;
        }

        var inferredType = context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type;

        if(inferredType != null && BuiltInTypes.Contains(inferredType.SpecialType))
        {
            Report(context, type.GetLocation(), inferredType.ToDisplayString());
        }
    }
}
