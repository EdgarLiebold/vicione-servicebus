using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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

/// <summary>Applies source-code fixes for cancellation token overload method.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public class CancellationTokenOverloadMethodFixer :
    CodeFixProvider
{
    /// <summary>Gets the fixable diagnostic ids.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CancellationTokenOverloadMethodRuleId);

    /// <summary>Gets fix all provider.</summary>
    /// <returns>The fix all provider.</returns>
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <summary>Registers code fixes.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        if (nodeToFix.IsKind(SyntaxKind.InvocationExpression))
        {
            if (!int.TryParse(context.Diagnostics[0].Properties[ParameterIndex], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var parameterIndex))
                return;

            if (!context.Diagnostics[0].Properties.TryGetValue(ParameterName, out var parameterName)
                || parameterName == null)
                return;

            if (!context.Diagnostics[0].Properties.TryGetValue(CancellationTokens, out var cancellationTokens)
                || cancellationTokens == null)
                return;

            foreach (var cancellationToken in cancellationTokens.Split(','))
            {
                var title = $"Forward the '{cancellationToken}' parameter to the methods";
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

        if (index > nodeToFix.ArgumentList.Arguments.Count)
        {
            SeparatedSyntaxList<ArgumentSyntax> newArguments =
                nodeToFix.ArgumentList.Arguments.Add((ArgumentSyntax)generator.Argument(parameterName, RefKind.None, expression));
            editor.ReplaceNode(nodeToFix.ArgumentList, nodeToFix.ArgumentList.WithArguments(newArguments));
        }
        else
        {
            SeparatedSyntaxList<ArgumentSyntax> newArguments =
                nodeToFix.ArgumentList.Arguments.Insert(index, (ArgumentSyntax)generator.Argument(expression));
            editor.ReplaceNode(nodeToFix.ArgumentList, nodeToFix.ArgumentList.WithArguments(newArguments));
        }

        return editor.GetChangedDocument();
    }
}
