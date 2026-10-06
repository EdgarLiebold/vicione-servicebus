using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Lazily materializes typed consume contexts from one deserialized message body.</summary>
public sealed class BodyConsumeContext :
    DeserializerConsumeContext
{
    readonly IDictionary<Type, ConsumeContext?> _messageTypes;

    /// <summary>Creates a consume context over a transport delivery and its serializer state.</summary>
    /// <param name="receiveContext">The transport delivery.</param>
    /// <param name="serializerContext">The deserialized body and metadata.</param>
    public BodyConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(
            receiveContext ?? throw new ArgumentNullException(nameof(receiveContext)),
            serializerContext ?? throw new ArgumentNullException(nameof(serializerContext)))
    {
        _messageTypes = new Dictionary<Type, ConsumeContext?>(1);
    }

    /// <summary>Gets the deserialized message identifier.</summary>
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <summary>Gets the deserialized request identifier.</summary>
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <summary>Gets the deserialized correlation identifier.</summary>
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <summary>Gets the deserialized conversation identifier.</summary>
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <summary>Gets the deserialized initiating message identifier.</summary>
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <summary>Gets the absolute message expiration time.</summary>
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <summary>Gets the logical source endpoint address, or null when none was supplied.</summary>
    public override Uri? SourceAddress => SerializerContext.SourceAddress;
    /// <summary>Gets the destination endpoint address, or null when none was supplied.</summary>
    public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
    /// <summary>Gets the response endpoint address, or null when none was supplied.</summary>
    public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
    /// <summary>Gets the fault endpoint address, or null when none was supplied.</summary>
    public override Uri? FaultAddress => SerializerContext.FaultAddress;
    /// <summary>Gets the envelope creation time.</summary>
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <summary>Gets the deserialized application headers.</summary>
    public override Headers Headers => SerializerContext.Headers;
    /// <summary>Gets metadata that identifies the sending host.</summary>
    public override HostInfo Host => SerializerContext.Host;
    /// <summary>Gets the declared message contract URNs.</summary>
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <summary>Determines whether the body can be consumed as a runtime message contract.</summary>
    /// <param name="messageType">The candidate message contract.</param>
    /// <returns><see langword="true" /> when the contract is declared or already materialized; otherwise, <see langword="false" />.</returns>
    public override bool HasMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        lock (_messageTypes)
        {
            if (_messageTypes.TryGetValue(messageType, out var existing))
                return existing != null;
        }

        return SerializerContext.IsSupportedMessageType(messageType);
    }

    /// <summary>Tries to materialize and cache a typed consume context for the body.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="message">The typed consume context when materialization succeeds.</param>
    /// <returns><see langword="true" /> when the body can be consumed as <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
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
