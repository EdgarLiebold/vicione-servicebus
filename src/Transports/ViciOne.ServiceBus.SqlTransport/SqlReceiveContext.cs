using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes a dequeued SQL transport record through the service-bus receive contract.</summary>
public sealed class SqlReceiveContext :
    BaseReceiveContext,
    SqlMessageContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;
    IHeaderProvider? _headerProvider;

    /// <summary>Creates a receive context for one locked SQL transport record.</summary>
    /// <param name="message">The dequeued transport record.</param>
    /// <param name="context">The owning receive-endpoint context.</param>
    /// <param name="settings">The source queue settings.</param>
    /// <param name="clientContext">The database client context.</param>
    /// <param name="connectionContext">The active SQL connection context.</param>
    /// <param name="lockContext">The delivery-lock context.</param>
    public SqlReceiveContext(SqlTransportMessage message, SqlReceiveEndpointContext context, ReceiveSettings settings, ClientContext clientContext,
        ConnectionContext connectionContext, SqlReceiveLockContext lockContext)
        : base(
            (message ?? throw new ArgumentNullException(nameof(message))).DeliveryCount > 1,
            context ?? throw new ArgumentNullException(nameof(context)),
            settings ?? throw new ArgumentNullException(nameof(settings)),
            clientContext ?? throw new ArgumentNullException(nameof(clientContext)),
            connectionContext ?? throw new ArgumentNullException(nameof(connectionContext)),
            lockContext ?? throw new ArgumentNullException(nameof(lockContext)))
    {
        TransportMessage = message;

        _body = CreateBody(message);
    }

    /// <summary>Gets the text, binary, or empty body after applying configured inbound limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the lazily created header provider for the transport record.</summary>
    protected override IHeaderProvider HeaderProvider => _headerProvider ??= new SqlHeaderProvider(TransportMessage);

    /// <summary>Gets the nonnegative delivery-record identifier as the transport sequence number.</summary>
    public ulong? SequenceNumber => checked((ulong)DeliveryMessageId);

    /// <summary>Gets the dequeued SQL transport record.</summary>
    public SqlTransportMessage TransportMessage { get; }

    /// <summary>Gets the optional routing key retained with the record.</summary>
    public string? RoutingKey => TransportMessage.RoutingKey;
    /// <summary>Gets the stable transport-message identifier.</summary>
    public Guid TransportMessageId => TransportMessage.TransportMessageId;

    /// <summary>Gets the consumer identifier that owns the delivery, when assigned.</summary>
    public Guid? ConsumerId => TransportMessage.ConsumerId;
    /// <summary>Gets the active delivery-lock identifier, when assigned.</summary>
    public Guid? LockId => TransportMessage.LockId;

    /// <summary>Gets the source queue name.</summary>
    public string QueueName => TransportMessage.QueueName;
    /// <summary>Gets the transport priority.</summary>
    public short Priority => TransportMessage.Priority;
    /// <summary>Gets the database delivery-record identifier.</summary>
    public long DeliveryMessageId => TransportMessage.MessageDeliveryId;
    /// <summary>Gets the instant at which the transport record was enqueued.</summary>
    public DateTimeOffset EnqueueTime => TransportMessage.EnqueueTime;
    /// <summary>Gets the delivery attempt count, including the current attempt.</summary>
    public int DeliveryCount => TransportMessage.DeliveryCount;

    /// <summary>Gets the optional partition key retained with the record.</summary>
    public string? PartitionKey => TransportMessage.PartitionKey;

    internal static MessageBody CreateBody(SqlTransportMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Body is not null && message.BinaryBody is not null)
            throw new InvalidDataException("A SQL transport record cannot contain both text and binary message bodies.");

        if (message.Body is { } text)
            return new StringMessageBody(text);

        return message.BinaryBody is { } content
            ? new BinaryMessageBody(content)
            : EmptyMessageBody.Instance;
    }

    /// <summary>Captures nondefault routing, partition, and priority values for replay.</summary>
    /// <returns>The transport-property bag, or <see langword="null" /> when every value is default.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        Dictionary<string, object>? properties = null;

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            (properties ??= [])[SqlTransportPropertyNames.RoutingKey] = RoutingKey;

        if (!string.IsNullOrWhiteSpace(PartitionKey))
            (properties ??= [])[SqlTransportPropertyNames.PartitionKey] = PartitionKey;

        if (Priority != 100)
            (properties ??= [])[SqlTransportPropertyNames.Priority] = Priority;

        return properties;
    }
}
