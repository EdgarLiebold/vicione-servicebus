using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a message type cache implementation.
/// </summary>
public static class MessageTypeCache
{
    static CachedType GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ => Activation.Activate(type, new Factory()));
    }

    /// <summary>
    /// Gets properties.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<PropertyInfo> GetProperties(Type type)
    {
        return GetOrAdd(type).Properties;
    }

    /// <summary>
    /// Determines whether valid message type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageType(Type type)
    {
        return GetOrAdd(type).IsValidMessageType;
    }

    /// <summary>
    /// Performs the invalid message type reason operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static string? InvalidMessageTypeReason(Type type)
    {
        return GetOrAdd(type).InvalidMessageTypeReason;
    }

    /// <summary>
    /// Determines whether temporary message type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsTemporaryMessageType(Type type)
    {
        return GetOrAdd(type).IsTemporaryMessageType;
    }

    /// <summary>
    /// Gets message types.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<Type> GetMessageTypes(Type type)
    {
        return GetOrAdd(type).MessageTypes;
    }

    /// <summary>
    /// Gets message type names.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<string> GetMessageTypeNames(Type type)
    {
        return GetOrAdd(type).MessageTypeNames;
    }


    readonly struct Factory :
        IActivationType<CachedType>
    {
        public CachedType ActivateType<T>()
            where T : class
        {
            return new CachedType<T>();
        }
    }


    static class Cached
    {
        internal static readonly ConcurrentDictionary<Type, CachedType> Instance = new ConcurrentDictionary<Type, CachedType>();
    }


    interface CachedType
    {
        bool IsTemporaryMessageType { get; }
        bool IsValidMessageType { get; }
        string? InvalidMessageTypeReason { get; }
        IReadOnlyList<Type> MessageTypes { get; }
        IReadOnlyList<string> MessageTypeNames { get; }
        IReadOnlyList<PropertyInfo> Properties { get; }
    }


    class CachedType<T> :
        CachedType
    {
        bool CachedType.IsTemporaryMessageType => MessageTypeCache<T>.IsTemporaryMessageType;
        bool CachedType.IsValidMessageType => MessageTypeCache<T>.IsValidMessageType;
        string? CachedType.InvalidMessageTypeReason => MessageTypeCache<T>.InvalidMessageTypeReason;
        public IReadOnlyList<Type> MessageTypes => MessageTypeCache<T>.MessageTypes;
        public IReadOnlyList<string> MessageTypeNames => MessageTypeCache<T>.MessageTypeNames;

        public IReadOnlyList<PropertyInfo> Properties => MessageTypeCache<T>.Properties;
    }
}


/// <summary>
/// Provides a message type cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageTypeCache<T> :
    IMessageTypeCache
{
    readonly Lazy<string> _diagnosticAddress;
    readonly Lazy<bool> _isTemporaryMessageType;
    readonly Lazy<bool> _isValidMessageType;
    readonly Lazy<IReadOnlyList<string>> _messageTypeNames;
    string? _invalidMessageTypeReason;
    IReadOnlyList<Type>? _messageTypes;
    IReadOnlyList<PropertyInfo>? _properties;

    MessageTypeCache()
    {
        _isValidMessageType = new Lazy<bool>(CheckIfValidMessageType);
        _isTemporaryMessageType = new Lazy<bool>(() => CheckIfTemporaryMessageType(typeof(T)));
        _messageTypeNames = new Lazy<IReadOnlyList<string>>(
            () => Array.AsReadOnly(GetMessageTypeNames().ToArray()));
        _diagnosticAddress = new Lazy<string>(GetDiagnosticAddress);
    }

    /// <summary>
    /// Gets the diagnostic address value.
    /// </summary>
    public static string DiagnosticAddress => Cached.Metadata.Value.DiagnosticAddress;
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public static IReadOnlyList<PropertyInfo> Properties => Cached.Metadata.Value.Properties;
    /// <summary>
    /// Gets the is valid message type value.
    /// </summary>
    public static bool IsValidMessageType => Cached.Metadata.Value.IsValidMessageType;
    /// <summary>
    /// Gets the invalid message type reason value.
    /// </summary>
    public static string? InvalidMessageTypeReason => Cached.Metadata.Value.InvalidMessageTypeReason;
    /// <summary>
    /// Gets the is temporary message type value.
    /// </summary>
    public static bool IsTemporaryMessageType => Cached.Metadata.Value.IsTemporaryMessageType;
    /// <summary>
    /// Gets the message types value.
    /// </summary>
    public static IReadOnlyList<Type> MessageTypes => Cached.Metadata.Value.MessageTypes;
    /// <summary>
    /// Gets the message type names value.
    /// </summary>
    public static IReadOnlyList<string> MessageTypeNames => Cached.Metadata.Value.MessageTypeNames;

    bool IMessageTypeCache.IsTemporaryMessageType => _isTemporaryMessageType.Value;

    IReadOnlyList<string> IMessageTypeCache.MessageTypeNames => _messageTypeNames.Value;
    string IMessageTypeCache.DiagnosticAddress => _diagnosticAddress.Value;
    IReadOnlyList<PropertyInfo> IMessageTypeCache.Properties => _properties ??= PropertyListFactory();
    bool IMessageTypeCache.IsValidMessageType => _isValidMessageType.Value;
    string? IMessageTypeCache.InvalidMessageTypeReason => _invalidMessageTypeReason;

    IReadOnlyList<Type> IMessageTypeCache.MessageTypes =>
        _messageTypes ??= Array.AsReadOnly(GetMessageTypes().ToArray());

    static IReadOnlyList<PropertyInfo> PropertyListFactory()
    {
        PropertyInfo[] properties = typeof(T).GetReadableInstanceProperties()
            .GroupBy(x => x.Name)
            .Select(x => x.Last())
            .ToArray();

        return Array.AsReadOnly(properties);
    }

    static bool CheckIfTemporaryMessageType(Type messageTypeInfo)
    {
        return (!messageTypeInfo.IsVisible && messageTypeInfo.IsClass)
            || (messageTypeInfo.IsGenericType && messageTypeInfo.GetGenericArguments().Any(x => CheckIfTemporaryMessageType(x)));
    }

    /// <summary>
    /// Returns all the message types that are available for the specified type. This will
    /// return any base classes or interfaces implemented by the type that are allowed
    /// message types.
    /// </summary>
    /// <returns>An enumeration of valid message types implemented by the specified type</returns>
    static IEnumerable<Type> GetMessageTypes()
    {
        if (IsValidMessageType)
            yield return typeof(T);

        if (typeof(T).TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] arguments))
        {
            foreach (var faultMessageType in MessageTypeCache.GetMessageTypes(arguments[0]))
            {
                var faultInterfaceType = typeof(Fault<>).MakeGenericType(faultMessageType);
                if (faultInterfaceType != typeof(T))
                    yield return faultInterfaceType;
            }
        }

        var baseType = typeof(T).BaseType;
        while (baseType != null && MessageTypeCache.IsValidMessageType(baseType))
        {
            yield return baseType;

            baseType = baseType.BaseType;
        }

        IEnumerable<Type>? interfaces = typeof(T)
            .GetInterfaces()
            .Where(MessageTypeCache.IsValidMessageType);

        foreach (var interfaceType in interfaces)
            yield return interfaceType;
    }

    /// <summary>
    /// Returns true if the specified type is an allowed message type, i.e.
    /// that it doesn't come from the .Net core assemblies or is without a namespace,
    /// amongst others.
    /// </summary>
    /// <returns>True if the message can be sent, otherwise false</returns>
    bool CheckIfValidMessageType()
    {
        var type = typeof(T);

        var ns = type.Namespace;
        if (ns == null)
        {
            if (type.IsAnonymousType())
            {
                _invalidMessageTypeReason = $"Message types must not be anonymous types: {TypeCache<T>.ShortName}";
                return false;
            }

            _invalidMessageTypeReason = $"Messages types must have a valid namespace: {TypeCache<T>.ShortName}";
            return false;
        }

        if (type is { Name: "JsonObject", Namespace: "System.Text.Json.Nodes" })
            return true;

        if (ns == "System" || ns.StartsWith("System."))
        {
            _invalidMessageTypeReason = $"Messages types must not be in the System namespace: {TypeCache<T>.ShortName}";
            return false;
        }

        if (typeof(object).Assembly.Equals(type.Assembly))
        {
            _invalidMessageTypeReason = $"Messages types must not be System types: {TypeCache<T>.ShortName}";
            return false;
        }

        if (type.ImplementsInterface<SendContext>()
            || type.ImplementsInterface<ConsumeContext>()
            || type.ImplementsInterface<ReceiveContext>())
        {
            _invalidMessageTypeReason = $"ConsumeContext, ReceiveContext, and SendContext are not valid message types: {TypeCache<T>.ShortName}";
            return false;
        }

        if (type.IsGenericType)
        {
            var typeDefinition = type.GetGenericTypeDefinition();
            if (typeDefinition.IsDefined(typeof(MessageContractExclusionAttribute), inherit: false))
            {
                _invalidMessageTypeReason = $"{TypeCache<T>.ShortName} is not a valid message type";
                return false;
            }

            if (type.IsOpenGeneric())
            {
                _invalidMessageTypeReason = $"Message types must not be open generic types: {TypeCache<T>.ShortName}";
                return false;
            }
        }

        return true;
    }

    static IEnumerable<string> GetMessageTypeNames()
    {
        return MessageTypes.Select(MessageUrn.ForTypeString);
    }

    static string GetDiagnosticAddress()
    {
        const string activity = "Activity";

        if (typeof(T).GetInterfaces().Any(static type =>
                type.IsDefined(typeof(ActivityContractAttribute), inherit: false)))
        {
            var activityName = typeof(T).Name;
            if (activityName.EndsWith(activity, StringComparison.InvariantCultureIgnoreCase))
                activityName = activityName.Substring(0, activityName.Length - activity.Length);

            return activityName;
        }

        var (type, ns, _) = MessageUrn.ForType<T>();
        return $"{type}/{ns}";
    }


    static class Cached
    {
        internal static readonly Lazy<IMessageTypeCache> Metadata = new Lazy<IMessageTypeCache>(() => new MessageTypeCache<T>());
    }
}
