using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Projects an already materialized mediator message through the common deserialization context contract.</summary>
/// <typeparam name="TMessage">The materialized message contract exposed by this context.</typeparam>
internal sealed class MediatorSerializationContext<TMessage> :
    BaseSerializerContext
    where TMessage : class
{
    readonly TMessage _message;

    /// <summary>Initializes a serialization view over an already materialized message.</summary>
    /// <param name="deserializer">The deserializer used for metadata and object projections.</param>
    /// <param name="context">The message metadata inherited from the send.</param>
    /// <param name="message">The already materialized message.</param>
    /// <param name="supportedMessageTypes">The contract identifiers implemented by the message.</param>
    public MediatorSerializationContext(IObjectDeserializer deserializer, MessageContext context, TMessage message, string[] supportedMessageTypes)
        : base(deserializer, context, supportedMessageTypes)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Returns the materialized message when it implements the requested contract.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the materialized message implements <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Returns the materialized message when it is assignable to the requested runtime type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the materialized message is assignable to <paramref name="messageType" />; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (messageType.IsInstanceOfType(_message))
        {
            message = _message;
            return true;
        }

        message = null;
        return false;
    }

    /// <summary>Rejects transport serialization because mediator dispatch has no transport envelope.</summary>
    /// <returns>This member never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown because no transport serializer exists.</exception>
    public override IMessageSerializer GetMessageSerializer()
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Rejects transport serialization because mediator dispatch has no transport envelope.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="envelope">The transport envelope that cannot be represented by the mediator.</param>
    /// <param name="message">The materialized message.</param>
    /// <returns>This member never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown because no transport serializer exists.</exception>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Rejects transport serialization because mediator dispatch has no transport envelope.</summary>
    /// <param name="message">The materialized message.</param>
    /// <param name="messageTypes">The declared transport contract identifiers.</param>
    /// <returns>This member never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown because no transport serializer exists.</exception>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        throw new NotSupportedException("The in-process mediator does not expose a transport message serializer.");
    }

    /// <summary>Projects a contract instance to the dictionary representation used by initializers.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The contract instance to project.</param>
    /// <returns>The converted dictionary.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return ConvertObject.ToDictionary(message);
    }
}
