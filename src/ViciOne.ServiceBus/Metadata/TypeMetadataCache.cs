using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached runtime metadata for message-contract types.</summary>
public static class TypeMetadataCache
{
    internal static IImplementationBuilder ImplementationBuilder => Cached.Builder;

    /// <summary>Gets the concrete runtime type used to materialize the supplied message contract.</summary>
    /// <param name="type">The message class or interface to materialize.</param>
    /// <returns>The original class type or the generated implementation of an interface contract.</returns>
    public static Type GetImplementationType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Cached.Builder.GetImplementationType(type);
    }

    /// <summary>Gets the canonical short diagnostic name of a runtime type.</summary>
    /// <param name="type">The runtime type to format.</param>
    /// <returns>The canonical short type name.</returns>
    public static string GetShortName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return TypeCache.GetShortName(type);
    }

    /// <summary>Gets the immutable set of public message properties exposed by a contract.</summary>
    /// <param name="type">The message contract to inspect.</param>
    /// <returns>The cached message property set.</returns>
    public static IReadOnlyList<PropertyInfo> GetProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return MessageTypeCache.GetProperties(type);
    }

    /// <summary>Determines whether a runtime type can be used as a message contract.</summary>
    /// <param name="type">The runtime type to validate.</param>
    /// <returns><see langword="true" /> when the type satisfies the message contract rules; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return MessageTypeCache.IsValidMessageType(type);
    }

    /// <summary>Determines whether a runtime type represents an anonymous or otherwise temporary message shape.</summary>
    /// <param name="type">The runtime type to classify.</param>
    /// <returns><see langword="true" /> for a temporary message type; otherwise, <see langword="false" />.</returns>
    public static bool IsTemporaryMessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return MessageTypeCache.IsTemporaryMessageType(type);
    }

    /// <summary>Gets the runtime type and implemented message contracts represented by a message.</summary>
    /// <param name="type">The runtime message type to inspect.</param>
    /// <returns>The immutable ordered set of represented message types.</returns>
    public static IReadOnlyList<Type> GetMessageTypes(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return MessageTypeCache.GetMessageTypes(type);
    }

    /// <summary>Gets the wire type identifiers represented by a message.</summary>
    /// <param name="type">The runtime message type to inspect.</param>
    /// <returns>The immutable ordered set of message type identifiers.</returns>
    public static IReadOnlyList<string> GetMessageTypeNames(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return MessageTypeCache.GetMessageTypeNames(type);
    }

    /// <summary>Determines whether a runtime type can be stored as object-valued message data.</summary>
    /// <param name="type">The runtime type to validate.</param>
    /// <returns><see langword="true" /> for a valid non-object reference message type; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageDataType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.IsInterfaceOrConcreteClass() && MessageTypeCache.IsValidMessageType(type) && !type.IsValueTypeOrObject();
    }
    static class Cached
    {
        internal static readonly IImplementationBuilder Builder = DynamicImplementationBuilder.Instance;
    }
}


/// <summary>Provides immutable cached metadata for a compile-time message-contract type.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public static class TypeMetadataCache<TMessage>
{
    static readonly Lazy<Type> ImplementationTypeCache = new(() => TypeMetadataCache.GetImplementationType(typeof(TMessage)));

    /// <summary>Gets the concrete runtime type used to materialize <typeparamref name="TMessage" />.</summary>
    public static Type ImplementationType => ImplementationTypeCache.Value;

    /// <summary>Gets the canonical short diagnostic name of <typeparamref name="TMessage" />.</summary>
    public static string ShortName => TypeCache<TMessage>.ShortName;

    /// <summary>Gets the canonical diagnostic address of <typeparamref name="TMessage" />.</summary>
    public static string DiagnosticAddress => MessageTypeCache<TMessage>.DiagnosticAddress;

    /// <summary>Gets the immutable public message properties exposed by <typeparamref name="TMessage" />.</summary>
    public static IReadOnlyList<PropertyInfo> Properties => MessageTypeCache<TMessage>.Properties;

    /// <summary>Gets a value indicating whether <typeparamref name="TMessage" /> satisfies the message-contract rules.</summary>
    public static bool IsValidMessageType => MessageTypeCache<TMessage>.IsValidMessageType;

    /// <summary>Gets the validation failure when <typeparamref name="TMessage" /> is not a valid message type.</summary>
    public static string? InvalidMessageTypeReason => MessageTypeCache<TMessage>.InvalidMessageTypeReason;

    /// <summary>Gets a value indicating whether <typeparamref name="TMessage" /> is an anonymous or otherwise temporary message shape.</summary>
    public static bool IsTemporaryMessageType => MessageTypeCache<TMessage>.IsTemporaryMessageType;

    /// <summary>Gets the immutable set of runtime message types represented by <typeparamref name="TMessage" />.</summary>
    public static IReadOnlyList<Type> MessageTypes => MessageTypeCache<TMessage>.MessageTypes;

    /// <summary>Gets the immutable set of wire type identifiers represented by <typeparamref name="TMessage" />.</summary>
    public static IReadOnlyList<string> MessageTypeNames => MessageTypeCache<TMessage>.MessageTypeNames;
}
