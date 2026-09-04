using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>
/// Provides a mediator serialization context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MediatorSerializationContext<TMessage> :
    BaseSerializerContext
    where TMessage : class
{
    readonly TMessage _message;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="deserializer">The deserializer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <param name="supportedMessageTypes">The supported message types value.</param>
    public MediatorSerializationContext(IObjectDeserializer deserializer, MessageContext context, TMessage message, string[] supportedMessageTypes)
        : base(deserializer, context, supportedMessageTypes)
    {
        _message = message;
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class
    {
        if (_message is T msg)
        {
            message = msg;
            return true;
        }

        message = null;
        return false;
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        if (messageType.IsAssignableFrom(typeof(TMessage)))
        {
            message = _message;
            return true;
        }

        message = null;
        return false;
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        throw new NotImplementedByDesignException();
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="envelope">The envelope value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        throw new NotImplementedByDesignException();
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        throw new NotImplementedByDesignException();
    }

    /// <summary>
    /// Performs the to dictionary operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        return ConvertObject.ToDictionary(message);
    }
}
