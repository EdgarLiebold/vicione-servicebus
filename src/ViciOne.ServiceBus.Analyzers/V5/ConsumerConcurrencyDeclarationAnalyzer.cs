#nullable enable

namespace ViciOne.ServiceBus.Analyzers.V5;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsumerConcurrencyDeclarationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "VOSB5003";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Prefer the first-class consumer concurrency contract",
        "ConsumerDefinition directly assigns '{0}'. Prefer ConsumerConcurrencyPolicy so concurrency intent cannot be confused with endpoint QoS.",
        "Configuration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

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
            || !AnalyzerSymbolFacts.IsConsumerDefinitionProperty(
                context.Compilation,
                property.Property,
                "ConcurrentMessageLimit"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(s_rule, assignment.Syntax.GetLocation(), property.Property.Name));
    }
}
