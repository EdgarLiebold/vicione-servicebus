using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Discovers message contracts for in-memory publish topology.</summary>
public static class InMemoryPublishTopologyConfigurationExtensions
{
    /// <summary>Adds valid message contracts from the namespace containing a marker type.</summary>
    /// <typeparam name="TMarker">The type whose assembly and namespace define the discovery boundary.</typeparam>
    /// <param name="configurator">The in-memory bus configurator to update.</param>
    /// <param name="configure">An optional callback invoked for each discovered message contract.</param>
    /// <param name="filter">An optional predicate that further restricts discovered contracts.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining<TMarker>(this IInMemoryBusFactoryConfigurator configurator,
        Action<IInMemoryMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        AddPublishMessageTypesFromNamespaceContaining(configurator, typeof(TMarker), configure, filter);
    }

    /// <summary>Adds valid message contracts from the namespace containing a runtime marker type.</summary>
    /// <param name="configurator">The in-memory bus configurator to update.</param>
    /// <param name="type">The type whose assembly and namespace define the discovery boundary.</param>
    /// <param name="configure">An optional callback invoked for each discovered message contract.</param>
    /// <param name="filter">An optional predicate that further restricts discovered contracts.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining(this IInMemoryBusFactoryConfigurator configurator, Type type,
        Action<IInMemoryMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(type);

        if (type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} does not declare a namespace.", nameof(type));

        IEnumerable<Type> types;

        const TypeClassification typeClassification = TypeClassification.Concrete | TypeClassification.Closed | TypeClassification.Abstract
            | TypeClassification.Interface;

        if (filter != null)
        {
            bool IsAllowed(Type candidate)
            {
                return MessageTypeCache.IsValidMessageType(candidate) && filter(candidate);
            }

            types = AssemblyTypeCache.FindTypesInNamespace(type, IsAllowed, typeClassification);
        }
        else
            types = AssemblyTypeCache.FindTypesInNamespace(type, MessageTypeCache.IsValidMessageType, typeClassification);

        foreach (var messageType in types)
            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
    }

    /// <summary>Adds an explicit set of message contracts to publish topology.</summary>
    /// <param name="configurator">The in-memory bus configurator to update.</param>
    /// <param name="messageTypes">The message contracts to add.</param>
    /// <param name="configure">An optional callback invoked for each message contract.</param>
    public static void AddPublishMessageTypes(this IInMemoryBusFactoryConfigurator configurator, IEnumerable<Type> messageTypes,
        Action<IInMemoryMessagePublishTopologyConfigurator, Type>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(messageTypes);

        foreach (var messageType in messageTypes)
        {
            if (messageType == null)
                throw new ArgumentException("The message type collection cannot contain null entries.", nameof(messageTypes));

            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
        }
    }
}
