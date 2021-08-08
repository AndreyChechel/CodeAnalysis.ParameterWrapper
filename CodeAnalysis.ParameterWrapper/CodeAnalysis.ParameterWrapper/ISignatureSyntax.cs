
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.ParameterWrapper
{
    public interface ISignatureSyntax
    {
        MemberDeclarationSyntax Node { get; }

        string Name { get; }

        int Line { get; }

        SyntaxToken Identifier { get; }

        TypeParameterListSyntax TypeParameterList { get; }

        ParameterListSyntax ParameterList { get; }

        ISignatureSyntax WithIdentifier(SyntaxToken identifier);

        ISignatureSyntax WithParameterList(ParameterListSyntax parameterList);

        ISignatureSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList);
    }
}
