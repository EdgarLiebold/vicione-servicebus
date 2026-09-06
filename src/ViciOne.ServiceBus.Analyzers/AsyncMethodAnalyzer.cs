using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViciOne.ServiceBus.Analyzers;

/// <summary>Analyzes source code for async method.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncMethodAnalyzer :
    DiagnosticAnalyzer
{
    /// <summary>Exposes the missing await rule id used by the containing type.</summary>
    public const string MissingAwaitRuleId = "VOSB1001";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor MissingAwaitRule = new DiagnosticDescriptor(MissingAwaitRuleId,
        "ViciOne.ServiceBus method is not awaited or captured",
        "Method {0} is not awaited or captured and may result in message loss",
        Category, DiagnosticSeverity.Warning, true,
        "ViciOne.ServiceBus method is not awaited or captured.");

    /// <summary>Gets the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingAwaitRule);

    /// <summary>Initializes the target component.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
