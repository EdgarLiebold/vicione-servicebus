using System;
using System.Collections.Generic;
using System.Net.Mime;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Projects an Azure Service Bus delivery into the transport receive-context abstractions.</summary>
public sealed class ServiceBusReceiveContext :
    BaseReceiveContext,
    ServiceBusMessageContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;
    readonly ServiceBusReceivedMessage _message;

    /// <summary>Initializes a receive context and marks redeliveries from the broker delivery count.</summary>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="receiveEndpointContext">The endpoint that received the message.</param>
    /// <param name="payloads">Additional transport payloads exposed through the context.</param>
    public ServiceBusReceiveContext(ServiceBusReceivedMessage message, ReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(message.DeliveryCount > 1, receiveEndpointContext, payloads)
    {
        _message = message;

        _body = new ServiceBusMessageBody(message.Body);
    }

    /// <summary>Gets a header provider over the received message.</summary>
    protected override IHeaderProvider HeaderProvider => new ServiceBusHeaderProvider(_message);

    /// <summary>Gets the received body after enforcing endpoint message-size limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    ulong? ITransportSequenceNumber.SequenceNumber => (ulong)SequenceNumber;

    /// <summary>Gets the broker message identifier.</summary>
    public string MessageId => _message.MessageId;

    /// <summary>Gets the application correlation identifier.</summary>
    public string CorrelationId => _message.CorrelationId;

    /// <summary>Gets the message time-to-live duration.</summary>
    public TimeSpan TimeToLive => _message.TimeToLive;

    /// <summary>Gets the UTC expiration time calculated by Azure Service Bus.</summary>
    public DateTimeOffset ExpiresAt => _message.ExpiresAt.UtcDateTime;

    /// <summary>Gets the application-defined message properties.</summary>
    public IReadOnlyDictionary<string, object> Properties => _message.ApplicationProperties;

    /// <summary>Gets the number of broker delivery attempts.</summary>
    public int DeliveryCount => _message.DeliveryCount;

    /// <summary>Gets the message subject exposed as the transport label.</summary>
    public string Label => _message.Subject;
    /// <summary>Gets the broker-assigned message sequence number.</summary>
    public long SequenceNumber => _message.SequenceNumber;

    /// <summary>Gets the original sequence number retained for an auto-forwarded message.</summary>
    public long EnqueuedSequenceNumber => _message.EnqueuedSequenceNumber;

    /// <summary>Gets the token that identifies this peek-lock delivery.</summary>
    public string LockToken => _message.LockToken;

    /// <summary>Gets the UTC time at which the current message lock expires.</summary>
    public DateTimeOffset LockedUntil => _message.LockedUntil.UtcDateTime;

    /// <summary>Gets the session identifier used for ordered session delivery.</summary>
    public string SessionId => _message.SessionId;

    /// <summary>Gets the received body length in bytes.</summary>
    public long Size => _message.Body?.ToMemory().Length ?? 0;

    /// <summary>Gets the application destination address carried by the message.</summary>
    public string To => _message.To;

    /// <summary>Gets the session identifier expected on replies.</summary>
    public string ReplyToSessionId => _message.ReplyToSessionId;

    /// <summary>Gets the key used to select a broker partition.</summary>
    public string PartitionKey => _message.PartitionKey;

    /// <summary>Gets the reply destination carried by the message.</summary>
    public string ReplyTo => _message.ReplyTo;

    /// <summary>Gets the UTC time at which Azure Service Bus accepted the message.</summary>
    public DateTimeOffset EnqueuedTime => _message.EnqueuedTime.UtcDateTime;

    /// <summary>Gets the UTC time at which the broker scheduled the message for enqueue.</summary>
    public DateTimeOffset ScheduledEnqueueTime => _message.ScheduledEnqueueTime.UtcDateTime;

    /// <summary>Captures non-empty provider-specific routing metadata for message movement or persistence.</summary>
    /// <returns>The transport property bag, or <see langword="null"/> when no provider-specific values are present.</returns>
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

    /// <summary>Parses the broker content type and falls back to base receive-context detection.</summary>
    /// <returns>The effective message content type.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = null;
        if (!string.IsNullOrWhiteSpace(_message.ContentType))
            contentType = ConvertToContentType(_message.ContentType);

        return contentType ?? base.GetContentType();
    }
}
