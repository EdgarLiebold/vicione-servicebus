using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Context;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a body consume context implementation.
/// </summary>
public class BodyConsumeContext :
    DeserializerConsumeContext
{
    readonly IDictionary<Type, ConsumeContext?> _messageTypes;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <param name="serializerContext">The serializer context value.</param>
    public BodyConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext, serializerContext)
    {
        _messageTypes = new Dictionary<Type, ConsumeContext?>(1);
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public override Uri SourceAddress => SerializerContext.SourceAddress!;
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public override Uri DestinationAddress => SerializerContext.DestinationAddress!;
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public override Uri ResponseAddress => SerializerContext.ResponseAddress!;
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public override Uri FaultAddress => SerializerContext.FaultAddress!;
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public override Headers Headers => SerializerContext.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public override HostInfo Host => SerializerContext.Host;
    /// <summary>
    /// Gets the supported message types value.
    /// </summary>
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <summary>
    /// Determines whether the current value has message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasMessageType(Type messageType)
    {
        lock (_messageTypes)
        {
            if (_messageTypes.TryGetValue(messageType, out var existing))
                return existing != null;
        }

        return SerializerContext.IsSupportedMessageType(messageType);
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? message)
    {
        lock (_messageTypes)
        {
            if (_messageTypes.TryGetValue(typeof(T), out var existing))
            {
                message = (existing as ConsumeContext<T>)!;
                return message != null;
            }

            if (typeof(T).IsInterface && MessageTypeCache<T>.IsValidMessageType)
            {
                if (SerializerContext.IsSupportedMessageType<T>())
                {
                    if (SerializerContext.TryGetMessage(typeof(T), out var messageObj))
                    {
                        message = new MessageConsumeContext<T>(this, (T)messageObj);
                        _messageTypes[typeof(T)] = message.Advanced();
                        return true;
                    }
                }
            }

            if (SerializerContext.TryGetMessage<T>(out var messageOfT))
            {
                message = new MessageConsumeContext<T>(this, messageOfT!);
                _messageTypes[typeof(T)] = message.Advanced();
                return true;
            }

            message = null;
            _messageTypes[typeof(T)] = null;
            return false;
        }
    }
}
