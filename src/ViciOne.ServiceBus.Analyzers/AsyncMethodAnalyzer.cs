using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>
/// Provides an async method analyzer implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncMethodAnalyzer :
    DiagnosticAnalyzer
{
    /// <summary>
    /// Defines the missing await rule id value.
    /// </summary>
    public const string MissingAwaitRuleId = "VOSB1001";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor MissingAwaitRule = new DiagnosticDescriptor(MissingAwaitRuleId,
        "ViciOne.ServiceBus method is not awaited or captured",
        "Method {0} is not awaited or captured and may result in message loss",
        Category, DiagnosticSeverity.Warning, true,
        "ViciOne.ServiceBus method is not awaited or captured.");

    /// <summary>
    /// Gets the supported diagnostics value.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingAwaitRule);

    /// <summary>
    /// Performs the initialize operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
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

            if (methodSymbol.IsProducerMethod(out _) && methodSymbol.ReturnsTask())
            {
                if (invocationExpression.Parent is ExpressionStatementSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingAwaitRule, invocationExpression.GetLocation(),
                        SymbolDisplay.ToDisplayString(methodSymbol,
                            SymbolDisplayFormat.CSharpShortErrorMessageFormat.WithParameterOptions(SymbolDisplayParameterOptions.None))));
                }
            }
        }
    }
}
