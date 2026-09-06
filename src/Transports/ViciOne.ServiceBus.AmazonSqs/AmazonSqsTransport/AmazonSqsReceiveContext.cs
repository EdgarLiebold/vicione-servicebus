using System;
using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Exposes a received Amazon SQS message to the receive pipeline.</summary>
public sealed class AmazonSqsReceiveContext :
    BaseReceiveContext,
    AmazonSqsMessageContext,
    TransportReceiveContext
{
    readonly MessageBody _body;
    readonly AmazonSqsHeaderProvider _headerProvider;

    /// <summary>Initializes a receive context for an Amazon SQS message.</summary>
    /// <param name="message">The message returned by Amazon SQS.</param>
    /// <param name="redelivered">Whether the message has previously been delivered.</param>
    /// <param name="context">The receive endpoint handling the message.</param>
    /// <param name="clientContext">The Amazon SQS client context for settlement operations.</param>
    /// <param name="settings">The queue receive settings.</param>
    /// <param name="connectionContext">The Amazon SQS connection context.</param>
    public AmazonSqsReceiveContext(Message message, bool redelivered, SqsReceiveEndpointContext context, ClientContext clientContext,
        ReceiveSettings settings, ConnectionContext connectionContext)
        : base(redelivered, context, settings, clientContext, connectionContext)
    {
        TransportMessage = message;
        TransportMessage.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();
        TransportMessage.Attributes ??= new Dictionary<string, string>();

        var messageBody = new SqsMessageBody(message);

        _body = messageBody;

        _headerProvider = new AmazonSqsHeaderProvider(TransportMessage, messageBody);
    }

    /// <summary>Gets the provider that reads envelope and Amazon SQS headers.</summary>
    protected override IHeaderProvider HeaderProvider => _headerProvider;

    /// <summary>Gets the received body after applying configured message-size limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the original Amazon SQS message.</summary>
    public Message TransportMessage { get; }

    /// <summary>Gets the custom message attributes returned by Amazon SQS.</summary>
    public Dictionary<string, MessageAttributeValue> Attributes => TransportMessage.MessageAttributes;

    /// <summary>Extracts non-empty FIFO group and deduplication identifiers for scheduled-message preservation.</summary>
    /// <returns>The available FIFO properties, or <see langword="null" /> when the message has none.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        Dictionary<string, object>? properties = null;

        if (TransportMessage.Attributes != null)
        {
            if (TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageGroupId, out var messageGroupId)
                && !string.IsNullOrWhiteSpace(messageGroupId))
                (properties ??= [])[AmazonSqsTransportPropertyNames.GroupId] = messageGroupId;

            if (TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageDeduplicationId, out var messageDeduplicationId)
                && !string.IsNullOrWhiteSpace(messageDeduplicationId))
                (properties ??= [])[AmazonSqsTransportPropertyNames.DeduplicationId] = messageDeduplicationId;
        }

        return properties;
    }
}
