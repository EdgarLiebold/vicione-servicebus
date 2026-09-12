using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Restores a persisted scheduled message onto the transport send context.</summary>
internal sealed class QuartzScheduledMessageSendPipe :
    IPipe<SendContext>
{
    readonly string _body;
    readonly ContentType _contentType;
    readonly Uri _destinationAddress;
    readonly QuartzScheduledMessageContext _messageContext;
    readonly string[] _supportedMessageTypes;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a send pipeline from persisted message content and metadata.</summary>
    /// <param name="contentType">The media type used to deserialize the persisted body.</param>
    /// <param name="messageContext">The persisted message metadata.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="destinationAddress">The destination used during deserialization.</param>
    /// <param name="supportedMessageTypes">The message contracts recorded with the scheduled message.</param>
    /// <param name="timeProvider">The clock used to calculate the remaining time to live.</param>
    internal QuartzScheduledMessageSendPipe(
        ContentType contentType,
        QuartzScheduledMessageContext messageContext,
        string body,
        Uri destinationAddress,
        string[] supportedMessageTypes,
        TimeProvider timeProvider)
    {
        _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        _messageContext = messageContext ?? throw new ArgumentNullException(nameof(messageContext));
        _body = body ?? throw new ArgumentNullException(nameof(body));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
        if (!destinationAddress.IsAbsoluteUri)
            throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destinationAddress));
        QuartzMessageTypeList.Validate(supportedMessageTypes, nameof(supportedMessageTypes));
        _supportedMessageTypes = supportedMessageTypes.ToArray();
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Restores the serialized message, standard metadata, headers and transport properties.</summary>
    /// <param name="context">The transport-bound send context.</param>
    /// <returns>A completed task after the send context has been populated.</returns>
    public Task SendAsync(SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var deserializer = context.Serialization.GetMessageDeserializer(_contentType);
            var body = deserializer.GetMessageBody(_body);
            var serializerContext = deserializer.Deserialize(body, _messageContext, _destinationAddress);

            context.MessageId = _messageContext.MessageId;
            context.RequestId = _messageContext.RequestId;
            context.ConversationId = _messageContext.ConversationId;
            context.CorrelationId = _messageContext.CorrelationId;
            context.InitiatorId = _messageContext.InitiatorId;
            context.SourceAddress = _messageContext.SourceAddress;
            context.ResponseAddress = _messageContext.ResponseAddress;
            context.FaultAddress = _messageContext.FaultAddress;

            if (_supportedMessageTypes.Length > 0)
                context.SupportedMessageTypes = _supportedMessageTypes;

            context.TimeToLive = ScheduledMessageExpiration.GetRemainingTimeToLive(_messageContext.ExpirationTime, _timeProvider);

            foreach (KeyValuePair<string, object> header in _messageContext.Headers.GetAll())
                context.Headers.Set(header.Key, header.Value);

            IReadOnlyDictionary<string, object>? transportProperties = _messageContext.TransportProperties;
            if (transportProperties is not null && context is TransportSendContext transportSendContext)
                transportSendContext.ReadPropertiesFrom(transportProperties);

            context.Serializer = serializerContext.GetMessageSerializer();

            return Task.CompletedTask;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidScheduledMessageDataException(
                "The persisted scheduled message could not be reconstructed for delivery.",
                exception);
        }
    }

    /// <summary>Contributes no additional diagnostic scope.</summary>
    /// <param name="context">The probe context associated with the send pipeline.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }
}
