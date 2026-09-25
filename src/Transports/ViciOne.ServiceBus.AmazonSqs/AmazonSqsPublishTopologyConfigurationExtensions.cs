using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SNS publish topology by scanning or enumerating message types.</summary>
public static class AmazonSqsPublishTopologyConfigurationExtensions
{
    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <typeparam name="T">A marker type whose assembly and namespace are scanned.</typeparam>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="configure">An optional callback that configures each discovered message type.</param>
    /// <param name="filter">An optional predicate that selects discovered message types.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining<T>(this IAmazonSqsBusFactoryConfigurator configurator,
        Action<IAmazonSqsMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
    {
        AddPublishMessageTypesFromNamespaceContaining(configurator, typeof(T), configure, filter);
    }

    /// <summary>Adds any valid message types found in the specified namespace to the publish topology.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="configure">An optional callback that configures each discovered message type.</param>
    /// <param name="filter">An optional predicate that selects discovered message types.</param>
    public static void AddPublishMessageTypesFromNamespaceContaining(this IAmazonSqsBusFactoryConfigurator configurator, Type type,
        Action<IAmazonSqsMessagePublishTopologyConfigurator, Type>? configure = null, Func<Type, bool>? filter = null)
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

        foreach (var messageType in types.ToArray())
            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
    }

    /// <summary>Adds the specified message types to the publish topology.</summary>
    /// <param name="configurator">The Amazon SQS bus configurator.</param>
    /// <param name="messageTypes">The message types to add.</param>
    /// <param name="configure">An optional callback that configures each supplied message type.</param>
    public static void AddPublishMessageTypes(this IAmazonSqsBusFactoryConfigurator configurator, IEnumerable<Type> messageTypes,
        Action<IAmazonSqsMessagePublishTopologyConfigurator, Type>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(messageTypes);

        Type[] types = messageTypes.ToArray();
        if (types.Any(type => type is null || !MessageTypeCache.IsValidMessageType(type)))
            throw new ArgumentException("The message type collection contains an invalid message contract.", nameof(messageTypes));

        foreach (var messageType in types)
            configurator.Publish(messageType, x => configure?.Invoke(x, messageType));
    }
}
