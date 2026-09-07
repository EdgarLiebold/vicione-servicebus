using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static ViciOne.ServiceBus.Analyzers.CancellationTokenOverloadMethodAnalyzer;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Forwards an available pipeline cancellation token to a cancellable overload.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class CancellationTokenOverloadMethodFixer :
    CodeFixProvider
{
    /// <summary>Gets the cancellation-forwarding diagnostic fixed by this provider.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CancellationTokenOverloadMethodRuleId);

    /// <summary>Gets the batch provider used for solution-wide cancellation forwarding.</summary>
    /// <returns>The standard batch fix-all provider.</returns>
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <summary>Registers one code action for each cancellation token visible at the invocation.</summary>
    /// <param name="context">The code-fix registration context.</param>
    /// <returns>A task that completes after registration.</returns>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        if (nodeToFix.IsKind(SyntaxKind.InvocationExpression))
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            if (!diagnostic.Properties.TryGetValue(ParameterIndex, out var parameterIndexText)
                || !int.TryParse(parameterIndexText, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var parameterIndex))
                return;

            if (!diagnostic.Properties.TryGetValue(ParameterName, out var parameterName)
                || parameterName == null)
                return;

            if (!diagnostic.Properties.TryGetValue(CancellationTokens, out var cancellationTokens)
                || cancellationTokens == null)
                return;

            foreach (var cancellationToken in cancellationTokens.Split(','))
            {
                var title = $"Forward '{cancellationToken}' to the cancellable overload";
                var codeAction = CodeAction.Create(
                    title,
                    ct => FixInvocationAsync(context.Document, (InvocationExpressionSyntax)nodeToFix, parameterIndex, parameterName, cancellationToken, ct),
                    title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }
    }

    static async Task<Document> FixInvocationAsync(Document document, InvocationExpressionSyntax nodeToFix, int index, string parameterName,
        string cancellationTokenExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var expression = SyntaxFactory.ParseExpression(cancellationTokenExpression);

        var arguments = nodeToFix.ArgumentList.Arguments;
        if (index > arguments.Count || arguments.Any(argument => argument.NameColon != null))
        {
            SeparatedSyntaxList<ArgumentSyntax> newArguments =
                arguments.Add((ArgumentSyntax)generator.Argument(parameterName, RefKind.None, expression));
            editor.ReplaceNode(nodeToFix.ArgumentList, nodeToFix.ArgumentList.WithArguments(newArguments));
        }
        else
        {
            SeparatedSyntaxList<ArgumentSyntax> newArguments =
                arguments.Insert(index, (ArgumentSyntax)generator.Argument(expression));
            editor.ReplaceNode(nodeToFix.ArgumentList, nodeToFix.ArgumentList.WithArguments(newArguments));
        }

        return editor.GetChangedDocument();
    }
}
