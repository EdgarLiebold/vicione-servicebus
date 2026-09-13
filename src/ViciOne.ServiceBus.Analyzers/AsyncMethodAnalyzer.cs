using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ViciOne.ServiceBus.Analyzers.Internals;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Reports unobserved asynchronous message-production calls.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodAnalyzer :
    DiagnosticAnalyzer
{
    /// <summary>Identifies producer tasks that are neither awaited nor captured.</summary>
    public const string MissingAwaitRuleId = "VOSB1001";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor MissingAwaitRule = new(MissingAwaitRuleId,
        "Observe the asynchronous ServiceBus operation",
        "Observe the task returned by '{0}' to prevent message loss",
        Category, DiagnosticSeverity.Warning, true,
        "Every asynchronous message-production operation must be awaited or retained until completion.");

    /// <summary>Gets the unobserved-producer-task diagnostic.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MissingAwaitRule];

    /// <summary>Registers invocation analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        var symbol = context.SemanticModel.GetSymbolInfo(invocationExpression);
        if (symbol.Symbol?.Kind == SymbolKind.Method)
        {
            var methodSymbol = (IMethodSymbol)symbol.Symbol;

            if (methodSymbol.IsProducerMethod()
                && methodSymbol.ReturnsTask()
                && IsUnobserved(invocationExpression, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingAwaitRule, invocationExpression.GetLocation(),
                    SymbolDisplay.ToDisplayString(methodSymbol,
                        SymbolDisplayFormat.CSharpShortErrorMessageFormat.WithParameterOptions(SymbolDisplayParameterOptions.None))));
            }
        }
    }

    static bool IsUnobserved(
        InvocationExpressionSyntax producerInvocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        SyntaxNode value = SkipTransparentWrappers(producerInvocation);
        if (TryGetConfigureAwaitInvocation(value, semanticModel, cancellationToken, out var configureAwait))
            value = SkipTransparentWrappers(configureAwait);

        if (value.Parent is ExpressionStatementSyntax)
            return true;

        return IsDiscardAssignment(value.Parent, semanticModel, cancellationToken);
    }

    static SyntaxNode SkipTransparentWrappers(SyntaxNode value)
    {
        while (value.Parent is ParenthesizedExpressionSyntax
               || value.Parent is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
            value = value.Parent;

        return value;
    }

    static bool TryGetConfigureAwaitInvocation(
        SyntaxNode value,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out InvocationExpressionSyntax invocation)
    {
        if (value.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax wrapper } memberAccess
            && ReferenceEquals(memberAccess.Expression, value)
            && semanticModel.GetSymbolInfo(wrapper, cancellationToken).Symbol is IMethodSymbol
            {
                Name: "ConfigureAwait",
                ContainingNamespace: { } containingNamespace,
            }
            && containingNamespace.ToDisplayString().Equals("System.Threading.Tasks", StringComparison.Ordinal))
        {
            invocation = wrapper;
            return true;
        }

        invocation = null!;
        return false;
    }

    static bool IsDiscardAssignment(
        SyntaxNode? parent,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        return parent is AssignmentExpressionSyntax
        {
            RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
            Left: IdentifierNameSyntax { Identifier.ValueText: "_" } discard,
        }
            && semanticModel.GetSymbolInfo(discard, cancellationToken).Symbol is null or IDiscardSymbol;
    }
}
