using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViciOne.ServiceBus.Analyzers.Rules;

/// <summary>Warns when message contracts carry potentially large binary content inline.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LargeInlinePayloadAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Identifies inline binary or stream members in message contracts.</summary>
    public const string DiagnosticId = "VOSB5005";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Potentially large binary payload is inline",
        "Message contract member '{0}' uses '{1}' inline; use MessageData/claim-check semantics for potentially large content",
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Large byte or stream members create broker-limit and resource-pressure risk.");

    /// <summary>Gets the large-inline-payload diagnostic.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers per-compilation contract analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
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

        if (ServiceBusSymbolFacts.IsConsumerType(context.Compilation, type))
        {
            foreach (INamedTypeSymbol message in ServiceBusSymbolFacts.ConsumedMessageTypes(context.Compilation, type))
                AnalyzeContract(context, message, reported);
        }

        if (ServiceBusSymbolFacts.HasCanonicalAttribute(
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
        foreach (ISymbol member in GetSerializableContractMembers(message))
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

    private static IEnumerable<ISymbol> GetSerializableContractMembers(INamedTypeSymbol message)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        IEnumerable<INamedTypeSymbol> contractTypes = message.TypeKind == TypeKind.Interface
            ? new[] { message }.Concat(message.AllInterfaces)
            : BaseTypes(message);

        foreach (INamedTypeSymbol contractType in contractTypes)
        {
            foreach (ISymbol member in contractType.GetMembers())
            {
                if (IsSerializableMember(member) && names.Add(member.Name))
                    yield return member;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> BaseTypes(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            yield return current;
    }

    private static bool IsSerializableMember(ISymbol member)
        => member switch
        {
            IPropertySymbol property => property.DeclaredAccessibility == Accessibility.Public
                && !property.IsStatic
                && property.GetMethod?.DeclaredAccessibility == Accessibility.Public
                && property.Parameters.IsEmpty,
            IFieldSymbol field => field.DeclaredAccessibility == Accessibility.Public
                && !field.IsStatic
                && !field.IsConst
                && !field.IsImplicitlyDeclared,
            _ => false,
        };

    private static bool IsLargeInlineType(Compilation compilation, ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array && array.ElementType.SpecialType == SpecialType.System_Byte)
            return true;

        if (type is not INamedTypeSymbol named)
            return false;

        if (ServiceBusSymbolFacts.IsCanonicalMessageData(compilation, named))
            return false;

        if (ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                compilation,
                named,
                "System.Memory`1",
                "System.ReadOnlyMemory`1")
            && named.TypeArguments.Length == 1
            && named.TypeArguments[0].SpecialType == SpecialType.System_Byte)
            return true;

        for (INamedTypeSymbol? current = named; current is not null; current = current.BaseType)
        {
            if (ServiceBusSymbolFacts.IsCanonicalFrameworkType(compilation, current, "System.IO.Stream"))
                return true;
        }

        return false;
    }
}
