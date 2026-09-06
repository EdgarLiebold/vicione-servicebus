using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;


namespace ViciOne.ServiceBus.Analyzers.V5;

/// <summary>
/// Provides a consumer concurrency declaration analyzer implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsumerConcurrencyDeclarationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Defines the diagnostic id value.
    /// </summary>
    public const string DiagnosticId = "VOSB5003";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Prefer the first-class consumer concurrency contract",
        "ConsumerDefinition directly assigns '{0}'. Prefer ConsumerConcurrencyPolicy so concurrency intent cannot be confused with endpoint QoS.",
        "Configuration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the supported diagnostics value.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>
    /// Performs the initialize operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        if (context.Operation is not ISimpleAssignmentOperation assignment
            || assignment.Target is not IPropertyReferenceOperation property
            || !AnalyzerSymbolFacts.IsConsumerDefinitionProperty(
                context.Compilation,
                property.Property,
                "ConcurrentMessageLimit"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(s_rule, assignment.Syntax.GetLocation(), property.Property.Name));
    }
}
