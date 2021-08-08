using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.ParameterWrapper
{
    public static class SyntaxExtensions
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

        public static bool TryGetSignature(this ParameterListSyntax parameterList, out ISignatureSyntax signature)
        {
            if (parameterList is null)
            {
                throw new ArgumentNullException(nameof(parameterList));
            }

            var parent = parameterList.Parent;

            if (parent is MethodDeclarationSyntax method)
            {
                signature = new MethodSignatureSyntax(method);
                return true;
            }

            if (parent is ConstructorDeclarationSyntax ctor)
            {
                signature = new ConstructorSignatureSyntax(ctor);
                return true;
            }

            if (parent is DelegateDeclarationSyntax del)
            {
                signature = new DelegateSignatureSyntax(del);
                return true;
            }

            signature = null;
            return false;
        }
    }
}
