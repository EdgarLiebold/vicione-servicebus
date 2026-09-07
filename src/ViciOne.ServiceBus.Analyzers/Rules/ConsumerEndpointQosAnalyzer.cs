using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Rules;

/// <summary>Keeps endpoint transport quality-of-service settings out of consumer definitions.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsumerEndpointQosAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Identifies consumer-owned endpoint prefetch assignments.</summary>
    public const string DiagnosticId = "VOSB5002";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Endpoint transport QoS is not consumer-owned",
        "ConsumerDefinition must not assign endpoint transport QoS property '{0}'; configure it on the receive endpoint",
        "Configuration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoint prefetch affects every consumer on a shared endpoint and must be owned by endpoint topology.");

    /// <summary>Gets the endpoint-quality-of-service ownership diagnostic.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers endpoint-property assignment analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
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
            || !(ServiceBusSymbolFacts.IsConsumerDefinitionProperty(
                    context.Compilation,
                    property.Property,
                    "PrefetchCount")
                || (ServiceBusSymbolFacts.IsReceiveEndpointConfiguratorProperty(
                        context.Compilation,
                        property.Property,
                        "PrefetchCount")
                    && ServiceBusSymbolFacts.IsInsideConsumerDefinition(
                        context.Compilation,
                        context.ContainingSymbol))))
            return;

        context.ReportDiagnostic(Diagnostic.Create(s_rule, assignment.Syntax.GetLocation(), property.Property.Name));
    }
}
