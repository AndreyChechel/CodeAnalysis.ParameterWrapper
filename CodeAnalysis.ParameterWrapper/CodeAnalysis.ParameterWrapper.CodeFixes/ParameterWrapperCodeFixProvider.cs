using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.ParameterWrapper
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterWrapperCodeFixProvider)), Shared]
    public class ParameterWrapperCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ParameterWrapperAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<ParameterListSyntax>()
                .First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => OrganizeParametersAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private Task<Solution> OrganizeParametersAsync(Document document, ParameterListSyntax parameterList, CancellationToken cancellationToken)
        {
            if (parameterList.TryGetSignature(out var signature))
            {
                return OrganizeParametersAsync(document, signature, parameterList, cancellationToken);
            }

            return Task.FromResult(document.Project.Solution);
        }

        private async Task<Solution> OrganizeParametersAsync(Document document, ISignatureSyntax signature, ParameterListSyntax paramListDecl, CancellationToken cancellationToken)
        {
            var updatedSignature = signature;

            if (paramListDecl.Parameters.Count == 0)
            {
                // Parentheses must be on the same line as the signature identifier
                updatedSignature = NormalizeSingleLineParentheses(updatedSignature);
            }
            else if (paramListDecl.Parameters.Count == 1)
            {
                // Parentheses and the parameter must be on the same line as the signature identifier
                updatedSignature = NormalizeSingleLineParentheses(updatedSignature);

                // Ensure the parameter is declared within a single line
                var param = updatedSignature.ParameterList.Parameters[0];
                var updatedParam = FormatSingleLineParameter(param);

                if (!ReferenceEquals(param, updatedParam))
                {
                    updatedSignature = updatedSignature.WithParameterList(SyntaxFactory
                        .ParameterList()
                        .AddParameters(updatedParam)
                    );
                }
            }
            else
            {
                // Resolve whitespace trivia used for the signature identation.
                // This whitespace will be used to indent the parameters.
                var indent = GetSignatureIndentation(updatedSignature);

                // Parentheses must be on the separate lines (as well as parameters)
                updatedSignature = NormalizeMultiLineParentheses(updatedSignature, indent);

                // Format parameters so they are declared on single lines
                var updatedParams = updatedSignature.ParameterList.Parameters
                    .Select(x => FormatSingleLineParameter(x))
                    .ToArray();

                // Create a parameter list where each parameter is declared on its own line
                var updatedParamsList = OrganizeMultiLineParameterList(updatedParams, indent);

                updatedSignature = updatedSignature.WithParameterList(
                    updatedSignature.ParameterList.WithParameters(updatedParamsList)
                );
            }

            // If there were changes, update the document
            if (!ReferenceEquals(signature, updatedSignature))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var updatedRoot = root.ReplaceNode(signature.Node, updatedSignature.Node);
                var updatedDocument = document.WithSyntaxRoot(updatedRoot);

                return updatedDocument.Project.Solution;
            }

            // No changes
            return document.Project.Solution;
        }

        private ISignatureSyntax NormalizeSingleLineParentheses(ISignatureSyntax signature)
        {
            var typeParamEnd = (signature.TypeParameterList?.GreaterThanToken).GetValueOrDefault();

            // Remove trivia between identifier and open-parenthesis(generic signature scenario):
            // Foo<T>{trivia}() -> Foo<T>()
            if (typeParamEnd.IsKind(SyntaxKind.GreaterThanToken))
            {
                if (typeParamEnd.HasTrailingTrivia)
                {
                    signature = signature.WithTypeParameterList(
                        signature.TypeParameterList.WithGreaterThanToken(typeParamEnd.WithTrailingTrivia())
                    );
                }
            }

            // Remove trivia between identifier and open-parenthesis(non-generic signature scenario):
            // Foo{trivia}() -> Foo()
            else if (signature.Identifier.HasTrailingTrivia)
            {
                signature = signature.WithIdentifier(
                    signature.Identifier.WithTrailingTrivia()
                );
            }

            // Remove trivia between identifier and open-parenthesis
            if (signature.ParameterList.OpenParenToken.HasLeadingTrivia)
            {
                signature = signature.WithParameterList(
                    signature.ParameterList.WithOpenParenToken(
                        signature.ParameterList.OpenParenToken.WithLeadingTrivia()
                    )
                );
            }

            return signature;
        }

        private ISignatureSyntax NormalizeMultiLineParentheses(ISignatureSyntax signature, SyntaxTrivia[] indent)
        {
            var eol = SyntaxFactory.EndOfLine(Environment.NewLine);
            var typeParamEnd = (signature.TypeParameterList?.GreaterThanToken).GetValueOrDefault();

            // Identifier must have trailing EOL (generic signature scenario):
            if (typeParamEnd.IsKind(SyntaxKind.GreaterThanToken))
            {
                signature = signature.WithTypeParameterList(
                    signature.TypeParameterList.WithGreaterThanToken(typeParamEnd.WithTrailingTrivia(eol))
                );
            }

            // Identifier must have trailing EOL (non-generic signature scenario):
            else
            {
                signature = signature.WithIdentifier(
                    signature.Identifier.WithTrailingTrivia(eol)
                );
            }

            // Open-parenthesis must be indented and have traling EOL
            // Close-paranthesis must be indented
            signature = signature.WithParameterList(
                signature.ParameterList
                    .WithOpenParenToken(
                        signature.ParameterList.OpenParenToken
                            .WithLeadingTrivia(indent)
                            .WithTrailingTrivia(eol)
                    )
                    .WithCloseParenToken(
                        signature.ParameterList.CloseParenToken
                            .WithLeadingTrivia(indent)
                    )
            );

            return signature;
        }

        private SeparatedSyntaxList<ParameterSyntax> OrganizeMultiLineParameterList(ParameterSyntax[] parameters, SyntaxTrivia[] indent)
        {
            var tabIndent = indent.Concat(new[] { SyntaxFactory.Whitespace("    ") }).ToArray();

            var eol = SyntaxFactory.EndOfLine(Environment.NewLine);

            var commaToken = SyntaxFactory
                .Token(SyntaxKind.CommaToken)
                .WithTrailingTrivia(eol);

            var paramsWithCommas = SyntaxFactory.NodeOrTokenList();
            var lastIndex = parameters.Length - 1;

            for (var i = 0; i <= lastIndex; i++)
            {
                var param = parameters[i];

                if (i == lastIndex)
                {
                    // The last parameter has no comma,
                    // so it requires an explicit EOL

                    param = param
                        .WithLeadingTrivia(tabIndent)
                        .WithTrailingTrivia(eol);

                    paramsWithCommas = paramsWithCommas
                        .Add(param);
                }
                else
                {
                    // Non-last parameter has a comma,
                    // no EOL is needed

                    param = param
                        .WithLeadingTrivia(tabIndent)
                        .WithTrailingTrivia();

                    paramsWithCommas = paramsWithCommas
                        .Add(param)
                        .Add(commaToken);
                }
            }

            var paramsList = SyntaxFactory
                .SeparatedList<ParameterSyntax>(paramsWithCommas);

            return paramsList;
        }

        private ParameterSyntax FormatSingleLineParameter(ParameterSyntax param)
        {
            // Transform every end-of-line trivia to a single space trivia

            var endOfLineTrivia = param
                .DescendantTrivia()
                .Where(x => x.IsKind(SyntaxKind.EndOfLineTrivia))
                .ToArray();

            if (endOfLineTrivia.Length == 0)
            {
                // No change is required
                return param;
            }

            return param.ReplaceTrivia(endOfLineTrivia, (t1, t2) =>
            {
                return SyntaxFactory.Space;
            });
        }

        private SyntaxTrivia[] GetSignatureIndentation(ISignatureSyntax signature)
        {
            // This method selects whitespace trivia declared
            // after the **last** EOL trivia.

            var indent = Array.Empty<SyntaxTrivia>();

            if (signature.Node.HasLeadingTrivia)
            {
                var lastNewLineIndex = -1;
                var leadingTrivia = signature.Node.GetLeadingTrivia();

                for (var i = 0; i < leadingTrivia.Count; i++)
                {
                    if (leadingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        lastNewLineIndex = i;
                    }
                }

                indent = leadingTrivia
                    .Skip(lastNewLineIndex + 1)
                    .Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia))
                    .ToArray();
            }

            return indent;
        }
    }
}
