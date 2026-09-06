using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ publish topology by scanning or enumerating message types.</summary>
public static class ActiveMqPublishTopologyConfigurationExtensions
{
    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <typeparam name="T">A marker type whose assembly and namespace are scanned.</typeparam>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="configure">An optional callback that configures each discovered message type.</param>
    /// <param name="filter">An optional predicate that selects discovered message types.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining<T>(this IActiveMqBusFactoryConfigurator configurator,
        Action<IActiveMqMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        AddPublishMessageTypesFromNamespaceContaining(configurator, typeof(T), configure, filter);
    }

    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="configure">An optional callback that configures each discovered message type.</param>
    /// <param name="filter">An optional predicate that selects discovered message types.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining(this IActiveMqBusFactoryConfigurator configurator, Type type,
        Action<IActiveMqMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

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

    /// <summary>Adds the specified message types to the publish topology.</summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="messageTypes">The message types to add.</param>
    /// <param name="configure">An optional callback that configures each supplied message type.</param>
    public static void AddPublishMessageTypes(this IActiveMqBusFactoryConfigurator configurator, IEnumerable<Type> messageTypes,
        Action<IActiveMqMessagePublishTopologyConfigurator, Type>? configure = null)
    {
        foreach (var messageType in messageTypes)
            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
    }
}
