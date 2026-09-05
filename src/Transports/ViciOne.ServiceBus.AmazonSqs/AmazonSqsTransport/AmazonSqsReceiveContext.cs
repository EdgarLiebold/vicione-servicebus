using System;
using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs receive context implementation.
/// </summary>
public sealed class AmazonSqsReceiveContext :
    BaseReceiveContext,
    AmazonSqsMessageContext,
    TransportReceiveContext
{
    readonly MessageBody _body;
    readonly AmazonSqsHeaderProvider _headerProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="redelivered">The redelivered value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="clientContext">The client context value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="connectionContext">The connection context value.</param>
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

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => _headerProvider;

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    public Message TransportMessage { get; }

    /// <summary>
    /// Gets the attributes value.
    /// </summary>
    public Dictionary<string, MessageAttributeValue> Attributes => TransportMessage.MessageAttributes;

    /// <summary>
    /// Gets transport properties.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDictionary<string, object> GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (TransportMessage.Attributes != null)
        {
            if (TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageGroupId, out var messageGroupId)
                && !string.IsNullOrWhiteSpace(messageGroupId))
                properties.Value[AmazonSqsTransportPropertyNames.GroupId] = messageGroupId;

            if (TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageDeduplicationId, out var messageDeduplicationId)
                && !string.IsNullOrWhiteSpace(messageDeduplicationId))
                properties.Value[AmazonSqsTransportPropertyNames.DeduplicationId] = messageDeduplicationId;
        }

        return properties.IsValueCreated ? properties.Value : null!;
    }
}
