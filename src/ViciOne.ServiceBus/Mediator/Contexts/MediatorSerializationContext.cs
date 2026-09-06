using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Carries state for mediator serialization operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MediatorSerializationContext<TMessage> :
    BaseSerializerContext
    where TMessage : class
{
    readonly TMessage _message;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="deserializer">The deserializer.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="supportedMessageTypes">The supported message types.</param>
    public MediatorSerializationContext(IObjectDeserializer deserializer, MessageContext context, TMessage message, string[] supportedMessageTypes)
        : base(deserializer, context, supportedMessageTypes)
    {
        _message = message;
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">Receives the message produced by the operation.</param>
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

    /// <summary>Attempts to get message.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
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

    /// <summary>Gets message serializer.</summary>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Gets message serializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="envelope">The envelope.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Gets message serializer.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Converts this value to dictionary.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted dictionary.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        return ConvertObject.ToDictionary(message);
    }
}
