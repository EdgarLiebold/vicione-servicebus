using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides extension methods for sql publish topology configuration.</summary>
public static class SqlPublishTopologyConfigurationExtensions
{
    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining<T>(this ISqlBusFactoryConfigurator configurator,
        Action<ISqlMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        AddPublishMessageTypesFromNamespaceContaining(configurator, typeof(T), configure, filter);
    }

    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining(this ISqlBusFactoryConfigurator configurator, Type type,
        Action<ISqlMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void AddPublishMessageTypes(this ISqlBusFactoryConfigurator configurator, IEnumerable<Type> messageTypes,
        Action<ISqlMessagePublishTopologyConfigurator, Type>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(messageTypes);

        var validatedMessageTypes = new List<Type>();
        foreach (var messageType in messageTypes)
        {
            if (messageType == null)
                throw new ArgumentException("The message type collection cannot contain null entries.", nameof(messageTypes));

            validatedMessageTypes.Add(messageType);
        }

        foreach (var messageType in validatedMessageTypes)
            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
    }
}
