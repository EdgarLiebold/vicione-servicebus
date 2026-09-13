using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Rules;

internal static class ServiceBusSymbolFacts
{
    private const string AbstractionsAssemblyName = "ViciOne.ServiceBus.Abstractions";
    private static readonly string[] s_consumerDefinitionMetadataNames =
    [
        "ViciOne.ServiceBus.Advanced.Registration.ConsumerDefinition",
        "ViciOne.ServiceBus.Advanced.Registration.ConsumerDefinition`1",
    ];

    public static bool IsConsumerType(Compilation compilation, INamedTypeSymbol? type)
        => type is not null && type.AllInterfaces.Any(contract => IsConsumerInterface(compilation, contract));

    public static IEnumerable<INamedTypeSymbol> ConsumedMessageTypes(Compilation compilation, INamedTypeSymbol type)
        => type.AllInterfaces
            .Where(contract => IsConsumerInterface(compilation, contract))
            .Select(static contract => contract.TypeArguments[0])
            .OfType<INamedTypeSymbol>();

    public static bool IsConsumerDefinitionProperty(
        Compilation compilation,
        IPropertySymbol property,
        string propertyName)
    {
        if (!string.Equals(property.Name, propertyName, StringComparison.Ordinal))
            return false;

        IPropertySymbol referencedProperty = property.OriginalDefinition;
        foreach (IAssemblySymbol assembly in GetAssemblies(compilation, AbstractionsAssemblyName))
        {
            foreach (string metadataName in s_consumerDefinitionMetadataNames)
            {
                INamedTypeSymbol? definition = assembly.GetTypeByMetadataName(metadataName);
                if (definition is null)
                    continue;

                foreach (IPropertySymbol candidate in definition.GetMembers(propertyName).OfType<IPropertySymbol>())
                {
                    if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, referencedProperty))
                        return true;
                }
            }
        }

        return false;
    }

    public static bool IsInsideConsumerImplementation(Compilation compilation, ISymbol? symbol)
    {
        IMethodSymbol? containingMethod = FindContainingOrdinaryMethod(symbol);
        INamedTypeSymbol? containingType = containingMethod?.ContainingType;
        if (containingMethod is null || containingType is null)
            return false;

        foreach (INamedTypeSymbol contract in containingType.AllInterfaces.Where(contract => IsConsumerInterface(compilation, contract)))
        {
            foreach (IMethodSymbol member in contract.GetMembers("ConsumeAsync").OfType<IMethodSymbol>())
            {
                ISymbol? implementation = containingType.FindImplementationForInterfaceMember(member);
                if (implementation is IMethodSymbol method
                    && SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, containingMethod.OriginalDefinition))
                    return true;
            }
        }

        return false;
    }

    public static bool IsTaskLikeResultOwner(Compilation compilation, ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        if (IsCanonicalFrameworkType(
                compilation,
                named,
                "System.Threading.Tasks.ValueTask",
                "System.Threading.Tasks.ValueTask`1"))
            return true;

        for (INamedTypeSymbol? current = named; current is not null; current = current.BaseType)
        {
            if (IsCanonicalFrameworkType(
                    compilation,
                    current,
                    "System.Threading.Tasks.Task",
                    "System.Threading.Tasks.Task`1"))
                return true;
        }

        return false;
    }

    public static IOperation? GetWriteTarget(IOperation operation)
        => operation switch
        {
            IAssignmentOperation assignment => assignment.Target,
            IIncrementOrDecrementOperation incrementOrDecrement => incrementOrDecrement.Target,
            _ => null,
        };

    public static bool IsCanonicalFrameworkType(
        Compilation compilation,
        INamedTypeSymbol type,
        params string[] metadataNames)
        => IsCanonicalFrameworkType(compilation, type, (IEnumerable<string>)metadataNames);

    public static bool IsCanonicalFrameworkType(
        Compilation compilation,
        INamedTypeSymbol type,
        IEnumerable<string> metadataNames)
    {
        foreach (string metadataName in metadataNames)
        {
            INamedTypeSymbol? canonical = compilation.GetTypeByMetadataName(metadataName);
            if (canonical is not null
                && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, canonical.OriginalDefinition))
                return true;
        }

        return false;
    }

    public static bool IsReceiveEndpointConfiguratorProperty(
        Compilation compilation,
        IPropertySymbol property,
        string propertyName)
    {
        if (!string.Equals(property.Name, propertyName, StringComparison.Ordinal))
            return false;

        INamedTypeSymbol? configurator = GetCanonicalAbstractionsType(
            compilation,
            "ViciOne.ServiceBus.Configuration.IReceiveEndpointConfigurator");
        return configurator is not null
            && configurator.GetMembers(propertyName).OfType<IPropertySymbol>().Any(candidate =>
                SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    property.OriginalDefinition));
    }

    public static bool IsInsideConsumerDefinition(Compilation compilation, ISymbol? symbol)
    {
        INamedTypeSymbol? containingType = symbol?.ContainingType;
        INamedTypeSymbol? canonical = GetCanonicalAbstractionsType(
            compilation,
            "ViciOne.ServiceBus.Advanced.Registration.ConsumerDefinition`1");
        if (containingType is null || canonical is null)
            return false;

        for (INamedTypeSymbol? current = containingType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, canonical.OriginalDefinition))
                return true;
        }

        return false;
    }

    public static bool HasCanonicalAttribute(Compilation compilation, INamedTypeSymbol type, string metadataName)
    {
        INamedTypeSymbol? attributeType = GetCanonicalAbstractionsType(compilation, metadataName);
        return attributeType is not null && type.GetAttributes().Any(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.OriginalDefinition, attributeType.OriginalDefinition));
    }

    public static bool IsCanonicalMessageData(Compilation compilation, INamedTypeSymbol type)
    {
        INamedTypeSymbol? messageData = GetCanonicalAbstractionsType(
            compilation,
            "ViciOne.ServiceBus.Advanced.Serialization.MessageData`1");
        return messageData is not null
            && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, messageData.OriginalDefinition);
    }

    private static bool IsConsumerInterface(Compilation compilation, INamedTypeSymbol contract)
    {
        if (contract.TypeArguments.Length != 1)
            return false;

        INamedTypeSymbol? canonical = GetCanonicalAbstractionsType(compilation, "ViciOne.ServiceBus.IConsumer`1");
        return canonical is not null
            && SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, canonical.OriginalDefinition);
    }

    private static INamedTypeSymbol? GetCanonicalAbstractionsType(Compilation compilation, string metadataName)
    {
        foreach (IAssemblySymbol assembly in GetAssemblies(compilation, AbstractionsAssemblyName))
        {
            INamedTypeSymbol? type = assembly.GetTypeByMetadataName(metadataName);
            if (type is not null)
                return type;
        }

        return null;
    }

    private static IEnumerable<IAssemblySymbol> GetAssemblies(Compilation compilation, string assemblyName)
    {
        if (string.Equals(compilation.Assembly.Name, assemblyName, StringComparison.Ordinal))
            yield return compilation.Assembly;

        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (string.Equals(assembly.Name, assemblyName, StringComparison.Ordinal))
                yield return assembly;
        }
    }

    private static IMethodSymbol? FindContainingOrdinaryMethod(ISymbol? symbol)
    {
        for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation } method)
                return method;
        }

        return null;
    }
}
