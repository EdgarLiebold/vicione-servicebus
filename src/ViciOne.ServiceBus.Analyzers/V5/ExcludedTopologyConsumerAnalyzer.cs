using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace ViciOne.ServiceBus.Analyzers.V5;

/// <summary>
/// Provides an excluded topology consumer analyzer implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExcludedTopologyConsumerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Defines the diagnostic id value.
    /// </summary>
    public const string DiagnosticId = "VOSB5004";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Consumer message is excluded from topology",
        "Consumer '{0}' consumes '{1}', which is excluded from automatic topology; provide an explicit compatible endpoint topology or remove the exclusion",
        "Topology",
        DiagnosticSeverity.Warning,
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
