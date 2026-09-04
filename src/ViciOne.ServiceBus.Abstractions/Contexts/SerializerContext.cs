using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for serializer context.
/// </summary>
public interface SerializerContext :
    MessageContext,
    IObjectDeserializer
{
    /// <summary>
    /// Gets the supported message types value.
    /// </summary>
    string[] SupportedMessageTypes { get; }

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsSupportedMessageType<T>()
        where T : class;

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsSupportedMessageType(Type messageType);

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class;

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message);

    /// <summary>
    /// Returns a message serializer using the deserialized message ContentType, that can be used to
    /// serialize the message on another <see cref="SendContext" />.
    /// </summary>
    /// <returns></returns>
    IMessageSerializer GetMessageSerializer();

    /// <summary>
    /// Returns a message serializer using the deserialized message ContentType, that can be used to
    /// serialize the message on another <see cref="SendContext" />.
    /// </summary>
    /// <param name="envelope">The message envelope to modify</param>
    /// <param name="message">A message to overlay on top of the existing message, merging the properties together</param>
    /// <returns></returns>
    IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
        where T : class;

    /// <summary>
    /// Returns a message serializer using the deserialized message ContentType, that can be used to
    /// serialize the message on another <see cref="SendContext" />.
    /// </summary>
    /// <param name="message">A message to overlay on top of the existing message, merging the properties together</param>
    /// <param name="messageTypes">The supported message types</param>
    /// <returns></returns>
    IMessageSerializer GetMessageSerializer(object message, string[] messageTypes);

    /// <summary>
    /// Converts a message (or really any object) to a dictionary of string, object. This is serializer dependent, since
    /// JSON serializers use internal objects for object properties, to encapsulate nested properties, etc.
    /// </summary>
    /// <param name="message">The message</param>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class;
}
