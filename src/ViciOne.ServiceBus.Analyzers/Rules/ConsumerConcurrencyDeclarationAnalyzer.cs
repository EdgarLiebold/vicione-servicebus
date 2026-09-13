using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Rules;

/// <summary>Directs consumer concurrency declarations to the first-class policy contract.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsumerConcurrencyDeclarationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Identifies direct writes to a consumer definition's concurrency limit.</summary>
    public const string DiagnosticId = "VOSB5003";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Prefer the first-class consumer concurrency contract",
        "ConsumerDefinition directly assigns '{0}'. Prefer ConsumerConcurrencyPolicy so concurrency intent cannot be confused with endpoint QoS.",
        "Configuration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Consumer concurrency belongs to the explicit consumer concurrency policy, not to consumer definition properties.");

    /// <summary>Gets the consumer-concurrency-policy diagnostic.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers property-write analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(
            AnalyzeWrite,
            OperationKind.SimpleAssignment,
            OperationKind.CompoundAssignment,
            OperationKind.CoalesceAssignment,
            OperationKind.Increment,
            OperationKind.Decrement);
    }

    private static void AnalyzeWrite(OperationAnalysisContext context)
    {
        if (ServiceBusSymbolFacts.GetWriteTarget(context.Operation) is not IPropertyReferenceOperation property
            || !ServiceBusSymbolFacts.IsConsumerDefinitionProperty(
                context.Compilation,
                property.Property,
                "ConcurrentMessageLimit"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Operation.Syntax.GetLocation(), property.Property.Name));
    }
}
