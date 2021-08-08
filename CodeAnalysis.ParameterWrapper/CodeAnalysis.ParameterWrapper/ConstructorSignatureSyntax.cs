using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.ParameterWrapper
{
    public sealed class ConstructorSignatureSyntax : ISignatureSyntax
    {
        public ConstructorDeclarationSyntax Node { get; }

        public string Name { get; }

        public int Line { get; }

        MemberDeclarationSyntax ISignatureSyntax.Node => Node;

        public SyntaxToken Identifier => Node.Identifier;

        public TypeParameterListSyntax TypeParameterList => null;

        public ParameterListSyntax ParameterList => Node.ParameterList;

        public ConstructorSignatureSyntax(ConstructorDeclarationSyntax node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));

            Name = Node.Identifier.ValueText + ".ctor";
            Line = Node.Identifier.GetLine();
        }

        public ISignatureSyntax WithIdentifier(SyntaxToken identifier)
        {
            var updated = Node.WithIdentifier(identifier);

            return new ConstructorSignatureSyntax(updated);
        }

        public ISignatureSyntax WithTypeParameterList(TypeParameterListSyntax typeParameterList)
        {
            throw new NotSupportedException();
        }

        public ISignatureSyntax WithParameterList(ParameterListSyntax parameterList)
        {
            var updated = Node.WithParameterList(parameterList);

            return new ConstructorSignatureSyntax(updated);
        }
    }
}
