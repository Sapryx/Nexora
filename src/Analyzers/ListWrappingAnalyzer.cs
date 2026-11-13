using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ListWrappingAnalyzer : StyleAnalyzer
{
    public ListWrappingAnalyzer()
        : base("NX0010", "List wrapping", "{0}")
    {
    }

    protected override void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeParameters, SyntaxKind.ParameterList);
        context.RegisterSyntaxNodeAction(AnalyzeArguments, SyntaxKind.ArgumentList);
        context.RegisterSyntaxNodeAction(AnalyzeCollection, SyntaxKind.CollectionExpression);
    }

    private static bool IsWrapped<T>(SyntaxToken open, SeparatedSyntaxList<T> items, SyntaxToken close)
        where T : SyntaxNode
    {
        int openLine = SyntaxLines.Of(open);
        return items.Any(it => SyntaxLines.Start(it) != openLine) ||
               SyntaxLines.Of(close) != SyntaxLines.End(items.Last());
    }

    private static bool IsOnePerLine<T>(SyntaxToken open, SeparatedSyntaxList<T> items) where T : SyntaxNode
    {
        int previousEnd = SyntaxLines.Of(open);

        foreach(var item in items)
        {
            if(SyntaxLines.Start(item) <= previousEnd)
            {
                return false;
            }

            previousEnd = SyntaxLines.End(item);
        }

        return true;
    }

    private static bool IsClosedOnOwnLine<T>(SyntaxToken open, SeparatedSyntaxList<T> items, SyntaxToken close)
        where T : SyntaxNode
    {
        return IsOnePerLine(open, items) && SyntaxLines.Of(close) > SyntaxLines.End(items.Last());
    }

    private void AnalyzeParameters(SyntaxNodeAnalysisContext context)
    {
        var list = (ParameterListSyntax)context.Node;
        var parameters = list.Parameters;

        if(parameters.Count > 0 &&
           IsWrapped(list.OpenParenToken, parameters, list.CloseParenToken) &&
           (!IsOnePerLine(list.OpenParenToken, parameters) ||
            SyntaxLines.Of(list.CloseParenToken) != SyntaxLines.End(parameters.Last())))
        {
            Report(
                context,
                list.OpenParenToken.GetLocation(),
                "Wrapped parameters go one per line after '(' with ')' on the last one"
            );
        }
    }

    private void AnalyzeArguments(SyntaxNodeAnalysisContext context)
    {
        var list = (ArgumentListSyntax)context.Node;

        if(list.Arguments.Count > 0 &&
           IsWrapped(list.OpenParenToken, list.Arguments, list.CloseParenToken) &&
           !IsClosedOnOwnLine(list.OpenParenToken, list.Arguments, list.CloseParenToken))
        {
            Report(
                context,
                list.OpenParenToken.GetLocation(),
                "Wrapped arguments go one per line after '(' with ')' on its own line"
            );
        }
    }

    private void AnalyzeCollection(SyntaxNodeAnalysisContext context)
    {
        var collection = (CollectionExpressionSyntax)context.Node;
        var open = collection.OpenBracketToken;

        if(collection.Elements.Count > 0 &&
           IsWrapped(open, collection.Elements, collection.CloseBracketToken) &&
           (!IsClosedOnOwnLine(open, collection.Elements, collection.CloseBracketToken) ||
            SyntaxLines.Of(open) != SyntaxLines.Of(open.GetPreviousToken())))
        {
            Report(
                context,
                open.GetLocation(),
                "Wrapped collections start with '[' on the declaration line, " +
                "go one item per line and end with ']' on its own line"
            );
        }
    }
}
