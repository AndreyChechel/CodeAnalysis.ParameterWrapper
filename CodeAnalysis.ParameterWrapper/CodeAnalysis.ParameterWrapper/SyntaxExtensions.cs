using Microsoft.CodeAnalysis;

namespace CodeAnalysis.ParameterWrapper
{
    internal static class SyntaxExtensions
    {
        public static int GetLine(this SyntaxToken token)
        {
            return token
                .GetLocation()
                .GetLineSpan()
                .StartLinePosition
                .Line;
        }

        public static int GetLine(this SyntaxNode node)
        {
            return node
                .GetLocation()
                .GetLineSpan()
                .StartLinePosition
                .Line;
        }
    }
}
