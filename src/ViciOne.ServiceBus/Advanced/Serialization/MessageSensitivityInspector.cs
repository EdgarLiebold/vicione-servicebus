using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Caches immutable sensitivity metadata without pinning collectible message assemblies.</summary>
public sealed class MessageSensitivityInspector : IMessageSensitivityInspector
{
    private readonly ConditionalWeakTable<Type, MessageSensitivityDescriptor> _cache = new();

    /// <summary>Gets the cached sensitivity descriptor for a message contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The immutable payload and member sensitivity descriptor.</returns>
    public MessageSensitivityDescriptor Inspect(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return _cache.GetValue(messageType, static type => CreateDescriptor(type));
    }

    private static MessageSensitivityDescriptor CreateDescriptor(Type messageType)
    {
        IReadOnlyCollection<Type> interfaces = EnumerateInterfaces(messageType).ToArray();
        bool sensitivePayload = EnumerateTypeHierarchy(messageType)
                .Any(static type => type.IsDefined(typeof(SensitivePayloadAttribute), inherit: false))
            || interfaces.Any(static contract => contract.IsDefined(typeof(SensitivePayloadAttribute), inherit: false));

        IEnumerable<string> typeMembers = EnumerateTypeHierarchy(messageType).SelectMany(static type =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(static property => property.IsDefined(typeof(SensitiveMemberAttribute), inherit: false))
                .Select(static property => property.Name)
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(static field => field.IsDefined(typeof(SensitiveMemberAttribute), inherit: false))
                    .Select(static field => field.Name)));

        IEnumerable<string> interfaceMembers = interfaces.SelectMany(static contract => contract
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static property => property.IsDefined(typeof(SensitiveMemberAttribute), inherit: false))
            .Select(static property => property.Name));

        FrozenSet<string> members = typeMembers.Concat(interfaceMembers).ToFrozenSet(StringComparer.Ordinal);
        return new MessageSensitivityDescriptor(
            sensitivePayload ? MessagePayloadSensitivity.Sensitive : MessagePayloadSensitivity.Normal,
            members);
    }

    private static IEnumerable<Type> EnumerateTypeHierarchy(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
            yield return current;
    }

    private static IEnumerable<Type> EnumerateInterfaces(Type type)
    {
        if (type.IsInterface)
            yield return type;

        foreach (Type contract in type.GetInterfaces())
            yield return contract;
    }
}
