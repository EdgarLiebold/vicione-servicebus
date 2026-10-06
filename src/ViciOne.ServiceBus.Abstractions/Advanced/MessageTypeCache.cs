using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides cached metadata for runtime message contract types.</summary>
public static class MessageTypeCache
{
    static CachedType GetOrAdd(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Cached.Instance.GetValue(type, CreateCachedType);
    }

    static CachedType CreateCachedType(Type type)
    {
        if (type.ContainsGenericParameters)
            return new InvalidCachedType($"Message types must not be open generic types: {type.GetTypeName()}");

        if (!type.IsClass && !type.IsInterface)
            return new InvalidCachedType($"Message types must be reference types: {type.GetTypeName()}");

        if (typeof(Delegate).IsAssignableFrom(type))
            return new InvalidCachedType($"Delegates are not valid message types: {type.GetTypeName()}");

        return Activation.Activate(type, new Factory());
    }

    /// <summary>Gets the readable instance properties exposed by a message contract.</summary>
    /// <param name="type">The runtime message contract type.</param>
    /// <returns>The cached property metadata.</returns>
    public static IReadOnlyList<PropertyInfo> GetProperties(Type type)
    {
        return GetOrAdd(type).Properties;
    }

    /// <summary>Determines whether a runtime type can be used as a message contract.</summary>
    /// <param name="type">The runtime type to evaluate.</param>
    /// <returns><see langword="true" /> when the type is a valid message contract; otherwise, <see langword="false" />.</returns>
    public static bool IsValidMessageType(Type type)
    {
        return GetOrAdd(type).IsValidMessageType;
    }

    /// <summary>Gets the reason a runtime type cannot be used as a message contract.</summary>
    /// <param name="type">The runtime type to evaluate.</param>
    /// <returns>The validation failure, or <see langword="null" /> when the type is valid.</returns>
    public static string? InvalidMessageTypeReason(Type type)
    {
        return GetOrAdd(type).InvalidMessageTypeReason;
    }

    /// <summary>Determines whether a contract or one of its generic arguments is a non-public class.</summary>
    /// <param name="type">The runtime message contract type.</param>
    /// <returns><see langword="true" /> when the contract is temporary; otherwise, <see langword="false" />.</returns>
    public static bool IsTemporaryMessageType(Type type)
    {
        return GetOrAdd(type).IsTemporaryMessageType;
    }

    /// <summary>Gets all valid contracts implemented by a runtime message type.</summary>
    /// <param name="type">The runtime message contract type.</param>
    /// <returns>The contract itself followed by its eligible fault, base, and interface contracts.</returns>
    public static IReadOnlyList<Type> GetMessageTypes(Type type)
    {
        return GetOrAdd(type).MessageTypes;
    }

    /// <summary>Gets canonical URNs for all valid contracts implemented by a runtime message type.</summary>
    /// <param name="type">The runtime message contract type.</param>
    /// <returns>The canonical message URNs in contract-discovery order.</returns>
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
        internal static readonly ConditionalWeakTable<Type, CachedType> Instance = new();
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


    sealed class CachedType<T> :
        CachedType
    {
        bool CachedType.IsTemporaryMessageType => MessageTypeCache<T>.IsTemporaryMessageType;
        bool CachedType.IsValidMessageType => MessageTypeCache<T>.IsValidMessageType;
        string? CachedType.InvalidMessageTypeReason => MessageTypeCache<T>.InvalidMessageTypeReason;
        public IReadOnlyList<Type> MessageTypes => MessageTypeCache<T>.MessageTypes;
        public IReadOnlyList<string> MessageTypeNames => MessageTypeCache<T>.MessageTypeNames;

        public IReadOnlyList<PropertyInfo> Properties => MessageTypeCache<T>.Properties;
    }


    sealed class InvalidCachedType :
        CachedType
    {
        static readonly IReadOnlyList<Type> _messageTypes = Array.AsReadOnly(Array.Empty<Type>());
        static readonly IReadOnlyList<string> _messageTypeNames = Array.AsReadOnly(Array.Empty<string>());
        static readonly IReadOnlyList<PropertyInfo> _properties = Array.AsReadOnly(Array.Empty<PropertyInfo>());
        readonly string _reason;

        public InvalidCachedType(string reason)
        {
            _reason = reason;
        }

        public bool IsTemporaryMessageType => false;
        public bool IsValidMessageType => false;
        public string InvalidMessageTypeReason => _reason;
        public IReadOnlyList<Type> MessageTypes => _messageTypes;
        public IReadOnlyList<string> MessageTypeNames => _messageTypeNames;
        public IReadOnlyList<PropertyInfo> Properties => _properties;
    }
}


/// <summary>Provides cached metadata for a compile-time message contract.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
public sealed class MessageTypeCache<T> :
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
        _isValidMessageType = new(CheckIfValidMessageType);
        _isTemporaryMessageType = new(() => CheckIfTemporaryMessageType(typeof(T)));
        _messageTypeNames = new(
            () => Array.AsReadOnly(GetMessageTypeNames().ToArray()));
        _diagnosticAddress = new(GetDiagnosticAddress);
    }

    /// <summary>Gets the compact contract name used in diagnostic scopes.</summary>
    public static string DiagnosticAddress => Cached.Metadata.Value.DiagnosticAddress;
    /// <summary>Gets the readable instance properties exposed by the contract.</summary>
    public static IReadOnlyList<PropertyInfo> Properties => Cached.Metadata.Value.Properties;
    /// <summary>Gets whether the type can be used as a message contract.</summary>
    public static bool IsValidMessageType => Cached.Metadata.Value.IsValidMessageType;
    /// <summary>Gets the validation failure, or <see langword="null" /> when the type is valid.</summary>
    public static string? InvalidMessageTypeReason => Cached.Metadata.Value.InvalidMessageTypeReason;
    /// <summary>Gets whether the contract or one of its generic arguments is a non-public class.</summary>
    public static bool IsTemporaryMessageType => Cached.Metadata.Value.IsTemporaryMessageType;
    /// <summary>Gets all valid contracts implemented by the message type.</summary>
    public static IReadOnlyList<Type> MessageTypes => Cached.Metadata.Value.MessageTypes;
    /// <summary>Gets canonical URNs for all valid contracts implemented by the message type.</summary>
    public static IReadOnlyList<string> MessageTypeNames => Cached.Metadata.Value.MessageTypeNames;

    bool IMessageTypeCache.IsTemporaryMessageType => _isTemporaryMessageType.Value;

    IReadOnlyList<string> IMessageTypeCache.MessageTypeNames => _messageTypeNames.Value;
    string IMessageTypeCache.DiagnosticAddress => _diagnosticAddress.Value;
    IReadOnlyList<PropertyInfo> IMessageTypeCache.Properties => _properties ??= PropertyListFactory();
    bool IMessageTypeCache.IsValidMessageType => _isValidMessageType.Value;
    string? IMessageTypeCache.InvalidMessageTypeReason
    {
        get
        {
            _ = _isValidMessageType.Value;
            return _invalidMessageTypeReason;
        }
    }

    IReadOnlyList<Type> IMessageTypeCache.MessageTypes =>
        _messageTypes ??= Array.AsReadOnly(GetMessageTypes().ToArray());

    static IReadOnlyList<PropertyInfo> PropertyListFactory()
    {
        Type type = typeof(T);
        if (type.ContainsGenericParameters || (!type.IsClass && !type.IsInterface) || typeof(Delegate).IsAssignableFrom(type))
            return Array.AsReadOnly(Array.Empty<PropertyInfo>());

        PropertyInfo[] properties = type.GetReadableInstanceProperties()
            .Where(property => property.GetIndexParameters().Length == 0)
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .ToArray();

        return Array.AsReadOnly(properties);
    }

    static bool CheckIfTemporaryMessageType(Type messageTypeInfo)
    {
        return (!messageTypeInfo.IsVisible && messageTypeInfo.IsClass)
            || (messageTypeInfo.IsGenericType && messageTypeInfo.GetGenericArguments().Any(x => CheckIfTemporaryMessageType(x)));
    }

    /// <summary>Enumerates the contract itself and its eligible fault, base, and interface contracts.</summary>
    /// <returns>The valid contracts implemented by <typeparamref name="T" />.</returns>
    static IEnumerable<Type> GetMessageTypes()
    {
        if (!IsValidMessageType)
            yield break;

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

    /// <summary>Validates the namespace, runtime origin, context role, and generic shape of the contract.</summary>
    /// <returns><see langword="true" /> when <typeparamref name="T" /> is an eligible message contract; otherwise, <see langword="false" />.</returns>
    bool CheckIfValidMessageType()
    {
        var type = typeof(T);

        if (!CheckReferenceShape(type))
            return false;

        if (!CheckNamespace(type, out bool isJsonObject))
            return false;

        if (isJsonObject)
            return true;

        return CheckContextRole(type) && CheckGenericShape(type);
    }

    bool CheckReferenceShape(Type type)
    {
        if (!type.IsClass && !type.IsInterface)
        {
            _invalidMessageTypeReason = $"Message types must be reference types: {TypeCache<T>.ShortName}";
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            _invalidMessageTypeReason = $"Delegates are not valid message types: {TypeCache<T>.ShortName}";
            return false;
        }

        return true;
    }

    bool CheckNamespace(Type type, out bool isJsonObject)
    {
        isJsonObject = false;
        var ns = type.Namespace;
        if (ns == null)
        {
            if (type.IsAnonymousType())
            {
                _invalidMessageTypeReason = $"Message types must not be anonymous types: {TypeCache<T>.ShortName}";
                return false;
            }

            _invalidMessageTypeReason = $"Message types must have a valid namespace: {TypeCache<T>.ShortName}";
            return false;
        }

        if (type == typeof(System.Text.Json.Nodes.JsonObject))
        {
            isJsonObject = true;
            return true;
        }

        if (ns == "System" || ns.StartsWith("System."))
        {
            _invalidMessageTypeReason = $"Message types must not be in the System namespace: {TypeCache<T>.ShortName}";
            return false;
        }

        if (typeof(object).Assembly.Equals(type.Assembly))
        {
            _invalidMessageTypeReason = $"Message types must not be System types: {TypeCache<T>.ShortName}";
            return false;
        }

        return true;
    }

    bool CheckContextRole(Type type)
    {
        if (type.ImplementsInterface<SendContext>()
            || type.ImplementsInterface<ConsumeContext>()
            || type.ImplementsInterface<ReceiveContext>())
        {
            _invalidMessageTypeReason = $"ConsumeContext, ReceiveContext, and SendContext are not valid message types: {TypeCache<T>.ShortName}";
            return false;
        }

        return true;
    }

    bool CheckGenericShape(Type type)
    {
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

        if (!IsValidMessageType)
            return TypeCache<T>.ShortName;

        if (typeof(T).GetInterfaces().Any(static type =>
                type.IsDefined(typeof(ActivityContractAttribute), inherit: false)))
        {
            var activityName = typeof(T).Name;
            if (activityName.EndsWith(activity, StringComparison.OrdinalIgnoreCase))
                activityName = activityName[..^activity.Length];

            return activityName;
        }

        MessageUrn urn = MessageUrn.ForType<T>();
        var (name, namespaceName, assemblyName) = urn;

        if (name is null)
            return urn.ToString();

        if (namespaceName is null)
            return name;

        return assemblyName is null
            ? $"{name}/{namespaceName}"
            : $"{name}/{namespaceName}/{assemblyName}";
    }


    static class Cached
    {
        internal static readonly Lazy<IMessageTypeCache> Metadata = new(() => new MessageTypeCache<T>());
    }
}
