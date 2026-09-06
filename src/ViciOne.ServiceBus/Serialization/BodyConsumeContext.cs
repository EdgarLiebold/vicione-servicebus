using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for body consume operations.</summary>
public class BodyConsumeContext :
    DeserializerConsumeContext
{
    readonly IDictionary<Type, ConsumeContext?> _messageTypes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <param name="serializerContext">The serializer context.</param>
    public BodyConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext, serializerContext)
    {
        _messageTypes = new Dictionary<Type, ConsumeContext?>(1);
    }

    /// <summary>Gets the message id.</summary>
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <summary>Gets the request id.</summary>
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <summary>Gets the conversation id.</summary>
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <summary>Gets the initiator id.</summary>
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <summary>Gets the expiration time.</summary>
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <summary>Gets the source address.</summary>
    public override Uri SourceAddress => SerializerContext.SourceAddress!;
    /// <summary>Gets the destination address.</summary>
    public override Uri DestinationAddress => SerializerContext.DestinationAddress!;
    /// <summary>Gets the response address.</summary>
    public override Uri ResponseAddress => SerializerContext.ResponseAddress!;
    /// <summary>Gets the fault address.</summary>
    public override Uri FaultAddress => SerializerContext.FaultAddress!;
    /// <summary>Gets the sent time.</summary>
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <summary>Gets the headers.</summary>
    public override Headers Headers => SerializerContext.Headers;
    /// <summary>Gets the host.</summary>
    public override HostInfo Host => SerializerContext.Host;
    /// <summary>Gets the supported message types.</summary>
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
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

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">Receives the message produced by the operation.</param>
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
