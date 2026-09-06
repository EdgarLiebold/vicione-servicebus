using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a type metadata cache implementation.
/// </summary>
public static class TypeMetadataCache
{
    static readonly object _builderLock = new object();
    internal static IImplementationBuilder ImplementationBuilder => Cached.Builder;

    /// <summary>
    /// Gets implementation type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static Type GetImplementationType(Type type)
    {
        lock (_builderLock)
            return Cached.Builder.GetImplementationType(type);
    }

    /// <summary>
    /// Gets short name.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static string GetShortName(Type type)
    {
        return TypeCache.GetShortName(type);
    }

    /// <summary>
    /// Gets properties.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<PropertyInfo> GetProperties(Type type)
    {
        return MessageTypeCache.GetProperties(type);
    }

    /// <summary>
    /// Determines whether valid message type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageType(Type type)
    {
        return MessageTypeCache.IsValidMessageType(type);
    }

    /// <summary>
    /// Determines whether temporary message type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsTemporaryMessageType(Type type)
    {
        return MessageTypeCache.IsTemporaryMessageType(type);
    }

    /// <summary>
    /// Gets message types.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<Type> GetMessageTypes(Type type)
    {
        return MessageTypeCache.GetMessageTypes(type);
    }

    /// <summary>
    /// Gets message type names.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<string> GetMessageTypeNames(Type type)
    {
        return MessageTypeCache.GetMessageTypeNames(type);
    }

    /// <summary>
    /// Determines whether valid message data type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageDataType(Type type)
    {
        return type.IsInterfaceOrConcreteClass() && MessageTypeCache.IsValidMessageType(type) && !type.IsValueTypeOrObject();
    }


    static class Cached
    {
        internal static readonly IImplementationBuilder Builder = DynamicImplementationBuilder.Instance;
    }
}


/// <summary>
/// Provides a type metadata cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TypeMetadataCache<T> :
    ITypeMetadataCache<T>
{
    readonly Lazy<Type> _implementationType;

    TypeMetadataCache()
    {
        _implementationType = new Lazy<Type>(() => TypeMetadataCache.GetImplementationType(typeof(T)));
    }

    /// <summary>
    /// Gets the implementation type value.
    /// </summary>
    public static Type ImplementationType => Cached.Metadata.Value.ImplementationType;

    /// <summary>
    /// Gets the short name value.
    /// </summary>
    public static string ShortName => TypeCache<T>.ShortName;
    /// <summary>
    /// Gets the diagnostic address value.
    /// </summary>
    public static string DiagnosticAddress => MessageTypeCache<T>.DiagnosticAddress;
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public static IReadOnlyList<PropertyInfo> Properties => MessageTypeCache<T>.Properties;
    /// <summary>
    /// Gets the is valid message type value.
    /// </summary>
    public static bool IsValidMessageType => MessageTypeCache<T>.IsValidMessageType;
    /// <summary>
    /// Gets the invalid message type reason value.
    /// </summary>
    public static string? InvalidMessageTypeReason => MessageTypeCache<T>.InvalidMessageTypeReason;
    /// <summary>
    /// Gets the is temporary message type value.
    /// </summary>
    public static bool IsTemporaryMessageType => MessageTypeCache<T>.IsTemporaryMessageType;
    /// <summary>
    /// Gets the message types value.
    /// </summary>
    public static IReadOnlyList<Type> MessageTypes => MessageTypeCache<T>.MessageTypes;
    /// <summary>
    /// Gets the message type names value.
    /// </summary>
    public static IReadOnlyList<string> MessageTypeNames => MessageTypeCache<T>.MessageTypeNames;

    Type ITypeMetadataCache<T>.ImplementationType => _implementationType.Value;


    static class Cached
    {
        internal static readonly Lazy<ITypeMetadataCache<T>> Metadata = new Lazy<ITypeMetadataCache<T>>(() => new TypeMetadataCache<T>());
    }
}
