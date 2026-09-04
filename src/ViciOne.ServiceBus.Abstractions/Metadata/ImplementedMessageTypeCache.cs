using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

public class ImplementedMessageTypeCache<TMessage> :
    IImplementedMessageTypeCache<TMessage>
    where TMessage : class
{
    readonly CachedType[] _implementedTypes;

    ImplementedMessageTypeCache()
    {
        _implementedTypes = GetMessageTypes()
            .Select(x => Activation.Activate(x.Type, new Factory(), x.Direct))
            .ToArray();
    }

    void IImplementedMessageTypeCache<TMessage>.EnumerateImplementedTypes(IImplementedMessageType implementedMessageType)
    {
        for (var i = 0; i < _implementedTypes.Length; i++)
        {
            if (_implementedTypes[i].MessageType == typeof(TMessage))
                continue;

            _implementedTypes[i].ImplementsType(implementedMessageType);
        }
    }

    /// <summary>
    /// Enumerate the implemented message types
    /// </summary>
    /// <param name="implementedMessageType">The interface reference to invoke for each type</param>
    public static void EnumerateImplementedTypes(IImplementedMessageType implementedMessageType)
    {
        Cached.Instance.Value.EnumerateImplementedTypes(implementedMessageType);
    }

    static IEnumerable<ImplementedType> GetMessageTypes()
    {
        var emittedTypes = new HashSet<Type>();

        foreach (var messageType in GetDirectTopologyTypes(typeof(TMessage)))
        {
            if (messageType != typeof(TMessage) && emittedTypes.Add(messageType))
                yield return new ImplementedType(messageType, true);
        }
    }

    /// <summary>
    /// Returns the immediate edges of the message topology graph. A class contributes its
    /// immediate base class and its most-specific valid interfaces. Interfaces inherited via
    /// the base class intentionally remain direct topology edges: an excluded base-class
    /// topology must not hide an independently valid message contract.
    /// </summary>
    static IEnumerable<Type> GetDirectTopologyTypes(Type messageType)
    {
        if (messageType.TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] arguments))
        {
            foreach (var implementedType in GetDirectTopologyTypes(arguments[0]))
                yield return typeof(Fault<>).MakeGenericType(implementedType);
        }

        var baseType = messageType.BaseType;
        if (baseType != null && baseType != typeof(object) && MessageTypeCache.IsValidMessageType(baseType))
            yield return baseType;

        var validInterfaces = messageType.GetInterfaces()
            .Where(MessageTypeCache.IsValidMessageType)
            .ToArray();

        var inheritedInterfaces = validInterfaces
            .SelectMany(interfaceType => interfaceType.GetInterfaces())
            .ToHashSet();

        foreach (var interfaceType in validInterfaces
                     .Where(interfaceType => !inheritedInterfaces.Contains(interfaceType))
                     .OrderBy(GetStableTypeName, StringComparer.Ordinal))
            yield return interfaceType;
    }

    static string GetStableTypeName(Type type)
    {
        return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
    }


    readonly struct Factory :
        IActivationType<CachedType, bool>
    {
        public CachedType ActivateType<T>(bool direct)
            where T : class
        {
            return new TypeAdapter<T>(direct);
        }
    }


    struct ImplementedType
    {
        /// <summary>
        /// The implemented type
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// True if the interface is directly implemented by the type
        /// </summary>
        public readonly bool Direct;

        public ImplementedType(Type type, bool direct)
        {
            Type = type;
            Direct = direct;
        }
    }


    interface CachedType
    {
        Type MessageType { get; }
        bool Direct { get; }
        void ImplementsType(IImplementedMessageType implementedMessageType);
    }


    static class Cached
    {
        internal static readonly Lazy<IImplementedMessageTypeCache<TMessage>> Instance = new(() => new ImplementedMessageTypeCache<TMessage>());
    }


    class TypeAdapter<TAdapter> :
        CachedType
        where TAdapter : class
    {
        public TypeAdapter(bool direct)
        {
            Direct = direct;
        }

        public bool Direct { get; }
        public Type MessageType => typeof(TAdapter);

        public void ImplementsType(IImplementedMessageType implementedMessageType)
        {
            implementedMessageType.ImplementsMessageType<TAdapter>(Direct);
        }
    }
}
