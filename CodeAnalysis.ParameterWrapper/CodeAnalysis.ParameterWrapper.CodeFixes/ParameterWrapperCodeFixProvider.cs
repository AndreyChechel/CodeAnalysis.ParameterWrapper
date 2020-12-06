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

        private Task<Solution> OrganizeParametersAsync(Document document, ParameterListSyntax paramListDecl, CancellationToken cancellationToken)
        {
            if (paramListDecl.Parent is MethodDeclarationSyntax method)
            {
                return OrganizeMethodParametersAsync(document, method, paramListDecl, cancellationToken);
            }
            else if (paramListDecl.Parent is ConstructorDeclarationSyntax ctor)
            {
                return OrganizeCtorParametersAsync(document, ctor, paramListDecl, cancellationToken);
            }

            return Task.FromResult(document.Project.Solution);
        }

        private async Task<Solution> OrganizeMethodParametersAsync(Document document, MethodDeclarationSyntax method, ParameterListSyntax paramListDecl, CancellationToken cancellationToken)
        {
            var updatedMethod = method;

            if (paramListDecl.Parameters.Count == 0)
            {
                // Parentheses must be on the same line as the method identifier
                updatedMethod = NormalizeMethodSingleLineParentheses(updatedMethod);
            }
            else if (paramListDecl.Parameters.Count == 1)
            {
                // Parentheses and the parameter must be on the same line as the method identifier
                updatedMethod = NormalizeMethodSingleLineParentheses(updatedMethod);

                // Ensure the parameter is declared within a single line
                var param = updatedMethod.ParameterList.Parameters[0];
                var updatedParam = FormatSingleLineParameter(param);

                if (!ReferenceEquals(param, updatedParam))
                {
                    updatedMethod = updatedMethod.WithParameterList(SyntaxFactory
                        .ParameterList()
                        .AddParameters(updatedParam)
                    );
                }
            }
            else
            {
                // Resolve whitespace trivia used for the method identation.
                // This whitespace will be used to indent the parameters.
                var methodIndent = GetDeclarationIndentation(updatedMethod);

                // Parentheses must be on the separate lines (as well as parameters)
                updatedMethod = NormalizeMethodMultiLineParentheses(updatedMethod, methodIndent);

                // Format parameters so they are declared on single lines
                var updatedParams = updatedMethod.ParameterList.Parameters
                    .Select(x => FormatSingleLineParameter(x))
                    .ToArray();

                // Create a parameter list where each parameter is declared on its own line
                var updatedParamsList = OrganizeMultiLineParameterList(updatedParams, methodIndent);

                updatedMethod = updatedMethod.WithParameterList(
                    updatedMethod.ParameterList.WithParameters(updatedParamsList)
                );
            }

            // If there were changes, update the document
            if (!ReferenceEquals(method, updatedMethod))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var updatedRoot = root.ReplaceNode(method, updatedMethod);
                var updatedDocument = document.WithSyntaxRoot(updatedRoot);

                return updatedDocument.Project.Solution;
            }

            // No changes
            return document.Project.Solution;
        }

        private async Task<Solution> OrganizeCtorParametersAsync(Document document, ConstructorDeclarationSyntax ctor, ParameterListSyntax paramListDecl, CancellationToken cancellationToken)
        {
            var updatedCtor = ctor;

            if (paramListDecl.Parameters.Count == 0)
            {
                // Parentheses must be on the same line as the method identifier
                updatedCtor = NormalizeCtorSingleLineParentheses(updatedCtor);
            }
            else if (paramListDecl.Parameters.Count == 1)
            {
                // Parentheses and the parameter must be on the same line as the method identifier
                updatedCtor = NormalizeCtorSingleLineParentheses(updatedCtor);

                // Ensure the parameter is declared within a single line
                var param = updatedCtor.ParameterList.Parameters[0];
                var updatedParam = FormatSingleLineParameter(param);

                if (!ReferenceEquals(param, updatedParam))
                {
                    updatedCtor = updatedCtor.WithParameterList(SyntaxFactory
                        .ParameterList()
                        .AddParameters(updatedParam)
                    );
                }
            }
            else
            {
                // Resolve whitespace trivia used for the method identation.
                // This whitespace will be used to indent the parameters.
                var methodIndent = GetDeclarationIndentation(updatedCtor);

                // Parentheses must be on the separate lines (as well as parameters)
                updatedCtor = NormalizeCtorMultiLineParentheses(updatedCtor, methodIndent);

                // Format parameters so they are declared on single lines
                var updatedParams = updatedCtor.ParameterList.Parameters
                    .Select(x => FormatSingleLineParameter(x))
                    .ToArray();

                // Create a parameter list where each parameter is declared on its own line
                var updatedParamsList = OrganizeMultiLineParameterList(updatedParams, methodIndent);

                updatedCtor = updatedCtor.WithParameterList(
                    updatedCtor.ParameterList.WithParameters(updatedParamsList)
                );
            }

            // If there were changes, update the document
            if (!ReferenceEquals(ctor, updatedCtor))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var updatedRoot = root.ReplaceNode(ctor, updatedCtor);
                var updatedDocument = document.WithSyntaxRoot(updatedRoot);

                return updatedDocument.Project.Solution;
            }

            // No changes
            return document.Project.Solution;
        }

        private MethodDeclarationSyntax NormalizeMethodSingleLineParentheses(MethodDeclarationSyntax method)
        {
            var typeParamEnd = (method.TypeParameterList?.GreaterThanToken).GetValueOrDefault();

            // Remove trivia between identifier and open-parenthesis(generic method scenario):
            // Foo<T>{trivia}() -> Foo<T>()
            if (typeParamEnd.IsKind(SyntaxKind.GreaterThanToken))
            {
                if (typeParamEnd.HasTrailingTrivia)
                {
                    method = method.WithTypeParameterList(
                        method.TypeParameterList.WithGreaterThanToken(typeParamEnd.WithTrailingTrivia())
                    );
                }
            }

            // Remove trivia between identifier and open-parenthesis(non-generic method scenario):
            // Foo{trivia}() -> Foo()
            else
            {
                if (method.Identifier.HasTrailingTrivia)
                {
                    method = method.WithIdentifier(
                        method.Identifier.WithTrailingTrivia()
                    );
                }
            }

            // Remove trivia between identifier and open-parenthesis
            if (method.ParameterList.OpenParenToken.HasLeadingTrivia)
            {
                method = method.WithParameterList(
                    method.ParameterList.WithOpenParenToken(
                        method.ParameterList.OpenParenToken.WithLeadingTrivia()
                    )
                );
            }

            return method;
        }

        private ConstructorDeclarationSyntax NormalizeCtorSingleLineParentheses(ConstructorDeclarationSyntax ctor)
        {
            // Remove trivia between identifier and open-parenthesis(non-generic method scenario):
            // Foo{trivia}() -> Foo()
            if (ctor.Identifier.HasTrailingTrivia)
            {
                ctor = ctor.WithIdentifier(
                    ctor.Identifier.WithTrailingTrivia()
                );
            }

            // Remove trivia between identifier and open-parenthesis
            if (ctor.ParameterList.OpenParenToken.HasLeadingTrivia)
            {
                ctor = ctor.WithParameterList(
                    ctor.ParameterList.WithOpenParenToken(
                        ctor.ParameterList.OpenParenToken.WithLeadingTrivia()
                    )
                );
            }

            return ctor;
        }

        private MethodDeclarationSyntax NormalizeMethodMultiLineParentheses(MethodDeclarationSyntax method, SyntaxTrivia[] indent)
        {
            var eol = SyntaxFactory.EndOfLine(Environment.NewLine);
            var typeParamEnd = (method.TypeParameterList?.GreaterThanToken).GetValueOrDefault();

            // Identifier must have trailing EOL (generic method scenario):
            if (typeParamEnd.IsKind(SyntaxKind.GreaterThanToken))
            {
                method = method.WithTypeParameterList(
                    method.TypeParameterList.WithGreaterThanToken(typeParamEnd.WithTrailingTrivia(eol))
                );
            }

            // Identifier must have trailing EOL (non-generic method scenario):
            else
            {
                method = method.WithIdentifier(
                    method.Identifier.WithTrailingTrivia(eol)
                );
            }

            // Open-parenthesis must be indented and have traling EOL
            // Close-paranthesis must be indented
            method = method.WithParameterList(
                method.ParameterList
                    .WithOpenParenToken(
                        method.ParameterList.OpenParenToken
                            .WithLeadingTrivia(indent)
                            .WithTrailingTrivia(eol)
                    )
                    .WithCloseParenToken(
                        method.ParameterList.CloseParenToken
                            .WithLeadingTrivia(indent)
                    )
            );

            return method;
        }

        private ConstructorDeclarationSyntax NormalizeCtorMultiLineParentheses(ConstructorDeclarationSyntax ctor, SyntaxTrivia[] indent)
        {
            var eol = SyntaxFactory.EndOfLine(Environment.NewLine);

            ctor = ctor.WithIdentifier(
                ctor.Identifier.WithTrailingTrivia(eol)
            );

            // Open-parenthesis must be indented and have traling EOL
            // Close-paranthesis must be indented
            ctor = ctor.WithParameterList(
                ctor.ParameterList
                    .WithOpenParenToken(
                        ctor.ParameterList.OpenParenToken
                            .WithLeadingTrivia(indent)
                            .WithTrailingTrivia(eol)
                    )
                    .WithCloseParenToken(
                        ctor.ParameterList.CloseParenToken
                            .WithLeadingTrivia(indent)
                    )
            );

            return ctor;
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

        private SyntaxTrivia[] GetDeclarationIndentation(BaseMethodDeclarationSyntax methodOrCtor)
        {
            // This method selects whitespace trivia declared
            // after the **last** EOL trivia.

            var methodIndent = Array.Empty<SyntaxTrivia>();

            if (methodOrCtor.HasLeadingTrivia)
            {
                var lastNewLineIndex = -1;
                var leadingTrivia = methodOrCtor.GetLeadingTrivia();

                for (var i = 0; i < leadingTrivia.Count; i++)
                {
                    if (leadingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        lastNewLineIndex = i;
                    }
                }

                methodIndent = leadingTrivia
                    .Skip(lastNewLineIndex + 1)
                    .Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia))
                    .ToArray();
            }

            return methodIndent;
        }
    }
}
