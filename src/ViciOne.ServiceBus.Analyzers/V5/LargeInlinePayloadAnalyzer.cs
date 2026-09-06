using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace ViciOne.ServiceBus.Analyzers.V5;

/// <summary>Analyzes source code for large inline payload.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LargeInlinePayloadAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Exposes the diagnostic id used by the containing type.</summary>
    public const string DiagnosticId = "VOSB5005";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Potentially large binary payload is inline",
        "Message contract member '{0}' uses '{1}' inline; use MessageData/claim-check semantics for potentially large content",
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Large byte or stream members create broker-limit and resource-pressure risk.");

    /// <summary>Gets the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Initializes the target component.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            var reported = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, reported),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        ConcurrentDictionary<ISymbol, byte> reported)
    {
        if (context.Symbol is not INamedTypeSymbol type)
            return;

        if (AnalyzerSymbolFacts.IsConsumerType(context.Compilation, type))
        {
            foreach (INamedTypeSymbol message in AnalyzerSymbolFacts.ConsumedMessageTypes(context.Compilation, type))
                AnalyzeContract(context, message, reported);
        }

        if (AnalyzerSymbolFacts.HasCanonicalAttribute(
                context.Compilation,
                type,
                "ViciOne.ServiceBus.MessageContractAttribute"))
            AnalyzeContract(context, type, reported);
    }

    private static void AnalyzeContract(
        SymbolAnalysisContext context,
        INamedTypeSymbol message,
        ConcurrentDictionary<ISymbol, byte> reported)
    {
        foreach (ISymbol member in GetContractMembers(message))
        {
            ITypeSymbol? memberType = member switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field when !field.IsImplicitlyDeclared => field.Type,
                _ => null,
            };
            if (memberType is null
                || !IsLargeInlineType(context.Compilation, memberType)
                || !reported.TryAdd(member, 0))
                continue;

            Location? location = member.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
            if (location is not null)
                context.ReportDiagnostic(Diagnostic.Create(s_rule, location, member.Name, memberType.ToDisplayString()));
        }
    }

    private static IEnumerable<ISymbol> GetContractMembers(INamedTypeSymbol message)
    {
        for (INamedTypeSymbol? current = message; current is not null; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers())
                yield return member;
        }

        foreach (INamedTypeSymbol contract in message.AllInterfaces)
        {
            foreach (ISymbol member in contract.GetMembers())
                yield return member;
        }
    }

    private static bool IsLargeInlineType(Compilation compilation, ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array && array.ElementType.SpecialType == SpecialType.System_Byte)
            return true;

        if (type is not INamedTypeSymbol named)
            return false;

        if (AnalyzerSymbolFacts.IsCanonicalMessageData(compilation, named))
            return false;

        if (AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                compilation,
                named,
                "System.Memory`1",
                "System.ReadOnlyMemory`1")
            && named.TypeArguments.Length == 1
            && named.TypeArguments[0].SpecialType == SpecialType.System_Byte)
            return true;

        for (INamedTypeSymbol? current = named; current is not null; current = current.BaseType)
        {
            if (AnalyzerSymbolFacts.IsCanonicalFrameworkType(compilation, current, "System.IO.Stream"))
                return true;
        }

        return false;
    }
}
