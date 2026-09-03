#nullable enable

namespace ViciOne.ServiceBus.Analyzers.V5;

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExcludedTopologyConsumerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "VOSB5004";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Consumer message is excluded from topology",
        "Consumer '{0}' consumes '{1}', which is excluded from automatic topology; provide an explicit compatible endpoint topology or remove the exclusion",
        "Topology",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol consumer
            || !AnalyzerSymbolFacts.IsConsumerType(context.Compilation, consumer))
            return;

        Location? location = consumer.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
        if (location is null)
            return;

        foreach (INamedTypeSymbol message in AnalyzerSymbolFacts.ConsumedMessageTypes(context.Compilation, consumer))
        {
            if (!AnalyzerSymbolFacts.HasCanonicalAttribute(
                    context.Compilation,
                    message,
                    "ViciOne.ServiceBus.ExcludeFromTopologyAttribute"))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                location,
                consumer.ToDisplayString(),
                message.ToDisplayString()));
        }
    }
}
