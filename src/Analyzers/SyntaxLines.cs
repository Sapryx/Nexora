using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzers;

public static class SyntaxLines
{
    public static int Start(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line;
    }

    public static int End(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().EndLinePosition.Line;
    }

    public static int Of(SyntaxToken token)
    {
        return token.GetLocation().GetLineSpan().StartLinePosition.Line;
    }

    public static int BlankLinesBefore(SyntaxToken token)
    {
        int count = 0;
        bool lineHasContent = false;

        foreach(var trivia in token.LeadingTrivia)
        {
            if(trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                if(!lineHasContent)
                {
                    count++;
                }

                lineHasContent = false;
            }
            else if(trivia.HasStructure)
            {
                lineHasContent = false;
            }
            else if(!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                lineHasContent = true;
            }
        }

        return count;
    }
}
