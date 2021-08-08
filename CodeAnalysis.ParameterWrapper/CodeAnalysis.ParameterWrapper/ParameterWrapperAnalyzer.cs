using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeAnalysis.ParameterWrapper
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterWrapperAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(ParameterWrapperAnalyzer);

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string _category = "Formatting/Indentation";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
        (
            DiagnosticId,
            _title,
            _messageFormat,
            _category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(_rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, new[] { SyntaxKind.ParameterList });
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var parameterList = (ParameterListSyntax)context.Node;

            // Ensure both parentheses are there
            if (!parameterList.OpenParenToken.IsKind(SyntaxKind.OpenParenToken) ||
                !parameterList.CloseParenToken.IsKind(SyntaxKind.CloseParenToken)
            )
            {
                return;
            }

            // Get signature node if declaration kind is supported.
            if (!parameterList.TryGetSignature(out var signature))
            {
                return;
            }

            // Get line number for the parentheses
            var declLine = signature.Line;
            var startBraceLine = parameterList.OpenParenToken.GetLine();
            var closeBraceLine = parameterList.CloseParenToken.GetLine();

            // Depending on the parameters count, verify whether formatting is optimal
            var parameters = parameterList.Parameters;

            if (parameters.Count <= 1)
            {
                // Braces must be on the same line as the parent declaration, e.g.:
                //
                //   void Foo()
                //   void Foo(object arg1)
                //

                if (declLine != startBraceLine || declLine != closeBraceLine)
                {
                    Report(context, parameterList, signature.Name);
                }
            }
            else
            {
                // Parameters must be declared on separate lines, e.g.:
                //
                //   void Foo
                //   (
                //       object arg1,
                //       object arg2
                //   )
                //

                // Verify the start/close brace position
                if (startBraceLine != declLine + 1 ||
                    closeBraceLine != startBraceLine + 1 + parameters.Count
                )
                {
                    Report(context, parameterList, signature.Name);
                }
                else
                {
                    // Verify the first parameter location
                    var paramLine = parameters[0].GetLine();
                    if (paramLine != startBraceLine + 1)
                    {
                        Report(context, parameterList, signature.Name);
                    }

                    // Verify the rest parameters are declared on their lines
                    for (int i = 1; i < parameters.Count; i++)
                    {
                        var currParamLine = parameters[i].GetLine();

                        if (currParamLine != paramLine + 1)
                        {
                            Report(context, parameterList, signature.Name);
                            break;
                        }

                        paramLine = currParamLine;
                    }
                }
            }
        }

        private static void Report(SyntaxNodeAnalysisContext context, ParameterListSyntax parameterList, string declName)
        {
            var diagnostic = Diagnostic.Create(_rule, parameterList.GetLocation(), declName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
