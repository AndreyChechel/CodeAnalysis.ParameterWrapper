using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.ParameterWrapper
{
    public sealed class DelegateSignatureSyntax : ISignatureSyntax
    {
        public DelegateDeclarationSyntax Node { get; }

        public string Name { get; }

        public int Line { get; }

        MemberDeclarationSyntax ISignatureSyntax.Node => Node;

        public SyntaxToken Identifier => Node.Identifier;

        public TypeParameterListSyntax TypeParameterList => Node.TypeParameterList;

        public ParameterListSyntax ParameterList => Node.ParameterList;

        public DelegateSignatureSyntax(DelegateDeclarationSyntax node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));

            Name = Node.Identifier.ValueText;
            Line = Node.Identifier.GetLine();
        }

        public ISignatureSyntax WithIdentifier(SyntaxToken identifier)
        {
            var updated = Node.WithIdentifier(identifier);

            return new DelegateSignatureSyntax(updated);
        }

        public ISignatureSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList)
        {
            var updated = Node.WithTypeParameterList(typeParameterList);

            return new DelegateSignatureSyntax(updated);
        }

        public ISignatureSyntax WithParameterList(ParameterListSyntax parameterList)
        {
            var updated = Node.WithParameterList(parameterList);

            return new DelegateSignatureSyntax(updated);
        }
    }
}
