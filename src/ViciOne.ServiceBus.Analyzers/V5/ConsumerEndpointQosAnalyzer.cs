using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace ViciOne.ServiceBus.Analyzers.V5;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsumerEndpointQosAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "VOSB5002";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Endpoint transport QoS is not consumer-owned",
        "ConsumerDefinition must not assign endpoint transport QoS property '{0}'; configure it on the receive endpoint",
        "Configuration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoint prefetch affects every consumer on a shared endpoint and must be owned by endpoint topology.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

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
            || !(AnalyzerSymbolFacts.IsConsumerDefinitionProperty(
                    context.Compilation,
                    property.Property,
                    "PrefetchCount")
                || (AnalyzerSymbolFacts.IsReceiveEndpointConfiguratorProperty(
                        context.Compilation,
                        property.Property,
                        "PrefetchCount")
                    && AnalyzerSymbolFacts.IsInsideConsumerDefinition(
                        context.Compilation,
                        context.ContainingSymbol))))
            return;

        context.ReportDiagnostic(Diagnostic.Create(s_rule, assignment.Syntax.GetLocation(), property.Property.Name));
    }
}
