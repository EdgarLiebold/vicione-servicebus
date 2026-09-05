using System;
using System.Collections.Generic;
using System.Net.Mime;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus receive context implementation.
/// </summary>
public sealed class ServiceBusReceiveContext :
    BaseReceiveContext,
    ServiceBusMessageContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;
    readonly ServiceBusReceivedMessage _message;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="payloads">The payloads value.</param>
    public ServiceBusReceiveContext(ServiceBusReceivedMessage message, ReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(message.DeliveryCount > 1, receiveEndpointContext, payloads)
    {
        _message = message;

        _body = new ServiceBusMessageBody(message.Body);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => new ServiceBusHeaderProvider(_message);

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    ulong? ITransportSequenceNumber.SequenceNumber => (ulong)SequenceNumber;

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public string MessageId => _message.MessageId;

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public string CorrelationId => _message.CorrelationId;

    /// <summary>
    /// Gets the time to live value.
    /// </summary>
    public TimeSpan TimeToLive => _message.TimeToLive;

    /// <summary>
    /// Gets the expires at value.
    /// </summary>
    public DateTimeOffset ExpiresAt => _message.ExpiresAt.UtcDateTime;

    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => _message.ApplicationProperties;

    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    public int DeliveryCount => _message.DeliveryCount;

    /// <summary>
    /// Gets the label value.
    /// </summary>
    public string Label => _message.Subject;
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    public long SequenceNumber => _message.SequenceNumber;

    /// <summary>
    /// Gets the enqueued sequence number value.
    /// </summary>
    public long EnqueuedSequenceNumber => _message.EnqueuedSequenceNumber;

    /// <summary>
    /// Gets the lock token value.
    /// </summary>
    public string LockToken => _message.LockToken;

    /// <summary>
    /// Gets the locked until value.
    /// </summary>
    public DateTimeOffset LockedUntil => _message.LockedUntil.UtcDateTime;

    /// <summary>
    /// Gets the session id value.
    /// </summary>
    public string SessionId => _message.SessionId;

    /// <summary>
    /// Gets the size value.
    /// </summary>
    public long Size => _message.Body?.ToMemory().Length ?? 0;

    /// <summary>
    /// Gets the to value.
    /// </summary>
    public string To => _message.To;

    /// <summary>
    /// Gets the reply to session id value.
    /// </summary>
    public string ReplyToSessionId => _message.ReplyToSessionId;

    /// <summary>
    /// Gets the partition key value.
    /// </summary>
    public string PartitionKey => _message.PartitionKey;

    /// <summary>
    /// Gets the reply to value.
    /// </summary>
    public string ReplyTo => _message.ReplyTo;

    /// <summary>
    /// Gets the enqueued time value.
    /// </summary>
    public DateTimeOffset EnqueuedTime => _message.EnqueuedTime.UtcDateTime;

    /// <summary>
    /// Gets the scheduled enqueue time value.
    /// </summary>
    public DateTimeOffset ScheduledEnqueueTime => _message.ScheduledEnqueueTime.UtcDateTime;

    /// <summary>
    /// Gets transport properties.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (!string.IsNullOrWhiteSpace(PartitionKey))
            properties.Value[AzureServiceBusTransportPropertyNames.PartitionKey] = PartitionKey;
        if (!string.IsNullOrWhiteSpace(SessionId))
            properties.Value[AzureServiceBusTransportPropertyNames.SessionId] = SessionId;
        if (!string.IsNullOrWhiteSpace(ReplyToSessionId))
            properties.Value[AzureServiceBusTransportPropertyNames.ReplyToSessionId] = ReplyToSessionId;
        if (!string.IsNullOrWhiteSpace(Label))
            properties.Value[AzureServiceBusTransportPropertyNames.Label] = Label;

        return properties.IsValueCreated ? properties.Value : null;
    }

    /// <summary>
    /// Gets content type.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = null;
        if (!string.IsNullOrWhiteSpace(_message.ContentType))
            contentType = ConvertToContentType(_message.ContentType);

        return contentType ?? base.GetContentType();
    }
}
