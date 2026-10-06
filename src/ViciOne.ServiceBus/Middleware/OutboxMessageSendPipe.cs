using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for outbox message send.</summary>
public class OutboxMessageSendPipe :
    IPipe<SendContext>
{
    readonly Uri? _destinationAddress;

    readonly OutboxMessageContext _message;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="destinationAddress">The destination address.</param>
    public OutboxMessageSendPipe(OutboxMessageContext message, Uri? destinationAddress)
    {
        _message = message;
        _destinationAddress = destinationAddress;
    }

    /// <summary>Reconstructs the stored outbox message metadata and serialized body in the send context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext context)
    {
        var contentType = new ContentType(_message.ContentType);

        var deserializer = context.Serialization.GetMessageDeserializer(contentType);

        var body = deserializer.GetMessageBody(_message.Body);
        DurablePayloadAdmissionProof? proof = (_message as IDurableOutboxMessageContext)?.AdmissionProof;
        byte[]? provedEnvelope = null;
        if (proof is { } durableProof)
        {
            provedEnvelope = body.ToArray();
            if (!durableProof.MatchesEnvelope(provedEnvelope, _message.ContentType))
                throw new SerializationException("The persisted outbox body or content type does not match its payload admission proof.");
        }

        var headers = new JsonTransportHeaders(new OutboxMessageHeaderProvider(_message));

        var serializerContext = deserializer.Deserialize(body, headers, _destinationAddress);

        context.MessageId = _message.MessageId;
        context.RequestId = _message.RequestId;
        context.ConversationId = _message.ConversationId;
        context.CorrelationId = _message.CorrelationId;
        context.InitiatorId = _message.InitiatorId;
        context.SourceAddress = _message.SourceAddress;
        context.ResponseAddress = _message.ResponseAddress;
        context.FaultAddress = _message.FaultAddress;
        context.SupportedMessageTypes = string.IsNullOrWhiteSpace(_message.MessageType)
            ? serializerContext.SupportedMessageTypes
            : _message.MessageType.Split(';').ToArray();

        if (_message.ExpirationTime.HasValue)
            context.TimeToLive = _message.ExpirationTime.Value.ToUniversalTime() - context.GetTimeProvider().GetUtcNow().UtcDateTime;

        foreach (var headerValue in headers)
        {
            if (headerValue.Key.StartsWith(MessageHeaders.Prefix, StringComparison.Ordinal))
                context.Headers.Set(headerValue.Key, headerValue.Value);
        }

        // A copied raw body does not rerun the raw serializer's header projection.
        // Restore only metadata headers that the original send actually persisted.
        RestoreRawGuidHeader(headers, context.Headers, MessageHeaders.CorrelationId, _message.CorrelationId);
        RestoreRawGuidHeader(headers, context.Headers, MessageHeaders.ConversationId, _message.ConversationId);
        RestoreRawGuidHeader(headers, context.Headers, MessageHeaders.RequestId, _message.RequestId);

        foreach (KeyValuePair<string, object> header in serializerContext.Headers.GetAll())
            context.Headers.Set(header.Key, header.Value);

        if (_message.Properties.Count > 0 && context is TransportSendContext transportSendContext)
            transportSendContext.ReadPropertiesFrom(_message.Properties);

        context.Serializer = provedEnvelope is not null
            ? new CopyBodySerializer(_message.ContentType, provedEnvelope, proof)
            : serializerContext.GetMessageSerializer();

        return Task.CompletedTask;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    static void RestoreRawGuidHeader(Headers persistedHeaders, SendHeaders sendHeaders, string name, Guid? value)
    {
        if (value.HasValue && persistedHeaders.TryGetHeader(name, out _))
            sendHeaders.Set(name, value.Value.ToString("D"));
    }


    class OutboxMessageHeaderProvider :
        IHeaderProvider
    {
        readonly OutboxMessageContext _message;

        public OutboxMessageHeaderProvider(OutboxMessageContext message)
        {
            _message = message;
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            yield return new KeyValuePair<string, object>(MessageHeaders.MessageId, _message.MessageId);

            if (!string.IsNullOrWhiteSpace(_message.ContentType))
                yield return new KeyValuePair<string, object>(MessageHeaders.ContentType, _message.ContentType!);

            foreach (KeyValuePair<string, object> header in _message.Headers.GetAll())
            {
                switch (header.Key)
                {
                    case MessageHeaders.MessageId:
                    case MessageHeaders.ContentType:
                        continue;

                    default:
                        yield return header;
                        break;
                }
            }
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            if (nameof(_message.MessageId).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = _message.MessageId;
                return true;
            }

            if (MessageHeaders.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = _message.ContentType;
                return true;
            }

            if (_message.Headers.TryGetHeader(key, out var headerValue))
            {
                value = headerValue;
                return true;
            }

            value = null;
            return false;
        }
    }
}
