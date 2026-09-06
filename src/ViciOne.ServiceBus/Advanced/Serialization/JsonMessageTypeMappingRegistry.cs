using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Registers JSON contract-to-implementation mappings contributed by optional capability packages.</summary>
public static class JsonMessageTypeMappingRegistry
{
    static readonly ConcurrentDictionary<Type, Type> ClosedMappings = new();
    static readonly ConcurrentDictionary<Type, Type> OpenMappings = new();

    /// <summary>Registers a closed message-contract mapping.</summary>
    /// <typeparam name="TContract">The contract type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    public static void Register<TContract, TImplementation>()
        where TContract : class
        where TImplementation : class, TContract =>
        Register(typeof(TContract), typeof(TImplementation), ClosedMappings, requireGenericDefinitions: false);

    /// <summary>Registers an open generic message-contract mapping.</summary>
    /// <param name="contractType">The open generic message-contract type.</param>
    /// <param name="implementationType">The open generic concrete implementation type.</param>
    public static void RegisterOpenGeneric(Type contractType, Type implementationType) =>
        Register(contractType, implementationType, OpenMappings, requireGenericDefinitions: true);

    internal static bool Contains(Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);

        return ClosedMappings.ContainsKey(contractType)
            || contractType.IsConstructedGenericType && OpenMappings.ContainsKey(contractType.GetGenericTypeDefinition());
    }

    internal static bool TryCreateConverter(Type contractType, [NotNullWhen(true)] out JsonConverter? converter)
    {
        ArgumentNullException.ThrowIfNull(contractType);

        if (ClosedMappings.TryGetValue(contractType, out Type? implementationType))
        {
            converter = CreateConverter(contractType, implementationType);
            return true;
        }

        if (contractType.IsConstructedGenericType
            && OpenMappings.TryGetValue(contractType.GetGenericTypeDefinition(), out Type? openImplementationType))
        {
            Type[] arguments = contractType.GetGenericArguments();
            implementationType = openImplementationType.MakeGenericType(arguments);
            converter = CreateConverter(contractType, implementationType);
            return true;
        }

        converter = null;
        return false;
    }

    static JsonConverter CreateConverter(Type contractType, Type implementationType) =>
        (JsonConverter)(Activator.CreateInstance(typeof(TypeMappingJsonConverter<,>).MakeGenericType(contractType, implementationType))
            ?? throw new InvalidOperationException("The requested JSON type-mapping converter could not be activated."));

    static void Register(Type contractType, Type implementationType, ConcurrentDictionary<Type, Type> mappings,
        bool requireGenericDefinitions)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(implementationType);

        if (contractType.IsGenericTypeDefinition != requireGenericDefinitions
            || implementationType.IsGenericTypeDefinition != requireGenericDefinitions)
        {
            throw new ArgumentException(requireGenericDefinitions
                ? "Both JSON mapping types must be open generic type definitions."
                : "Closed JSON mapping types cannot be open generic type definitions.");
        }

        if (requireGenericDefinitions
            && contractType.GetGenericArguments().Length != implementationType.GetGenericArguments().Length)
            throw new ArgumentException("Open generic JSON mapping types must have the same generic arity.");

        if (!implementationType.IsClass || implementationType.IsAbstract)
            throw new ArgumentException("The JSON mapping implementation must be a concrete class.", nameof(implementationType));

        if (requireGenericDefinitions && !ImplementsOpenContract(contractType, implementationType))
        {
            throw new ArgumentException(
                $"The open generic implementation '{implementationType}' must implement '{contractType}' using the same generic arguments.",
                nameof(implementationType));
        }

        Type registered = mappings.GetOrAdd(contractType, implementationType);
        if (registered != implementationType)
        {
            throw new InvalidOperationException(
                $"JSON contract '{contractType.FullName}' is already mapped to '{registered.FullName}'.");
        }
    }

    static bool ImplementsOpenContract(Type contractType, Type implementationType)
    {
        Type[] implementationArguments = implementationType.GetGenericArguments();
        if (contractType.IsInterface)
        {
            return implementationType.GetInterfaces().Any(candidate =>
                HasMatchingOpenContract(candidate, contractType, implementationArguments));
        }

        for (Type? candidate = implementationType.BaseType; candidate is not null; candidate = candidate.BaseType)
        {
            if (HasMatchingOpenContract(candidate, contractType, implementationArguments))
                return true;
        }

        return false;
    }

    static bool HasMatchingOpenContract(Type candidate, Type contractType, Type[] implementationArguments)
    {
        return candidate.IsGenericType
            && candidate.GetGenericTypeDefinition() == contractType
            && candidate.GetGenericArguments().SequenceEqual(implementationArguments);
    }
}
