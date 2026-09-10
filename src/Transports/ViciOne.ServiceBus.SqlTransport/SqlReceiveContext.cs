using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for sql receive operations.</summary>
public sealed class SqlReceiveContext :
    BaseReceiveContext,
    SqlMessageContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;
    IHeaderProvider? _headerProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="clientContext">The client context.</param>
    /// <param name="connectionContext">The connection context.</param>
    /// <param name="lockContext">The lock context.</param>
    public SqlReceiveContext(SqlTransportMessage message, SqlReceiveEndpointContext context, ReceiveSettings settings, ClientContext clientContext,
        ConnectionContext connectionContext, SqlReceiveLockContext lockContext)
        : base(
            (message ?? throw new ArgumentNullException(nameof(message))).DeliveryCount > 0,
            context ?? throw new ArgumentNullException(nameof(context)),
            settings ?? throw new ArgumentNullException(nameof(settings)),
            clientContext ?? throw new ArgumentNullException(nameof(clientContext)),
            connectionContext ?? throw new ArgumentNullException(nameof(connectionContext)),
            lockContext ?? throw new ArgumentNullException(nameof(lockContext)))
    {
        TransportMessage = message;

        _body = message.Body != null
            ? new StringMessageBody(message.Body)
            : new BytesMessageBody(message.BinaryBody);
    }

    /// <summary>Gets the body.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the header provider.</summary>
    protected override IHeaderProvider HeaderProvider => _headerProvider ??= new SqlHeaderProvider(TransportMessage);

    /// <summary>Gets the sequence number.</summary>
    public ulong? SequenceNumber => checked((ulong)DeliveryMessageId);

    /// <summary>Gets the transport message.</summary>
    public SqlTransportMessage TransportMessage { get; }

    /// <summary>Gets the routing key.</summary>
    public string? RoutingKey => TransportMessage.RoutingKey;
    /// <summary>Gets the transport message id.</summary>
    public Guid TransportMessageId => TransportMessage.TransportMessageId;

    /// <summary>Gets the consumer id.</summary>
    public Guid? ConsumerId => TransportMessage.ConsumerId;
    /// <summary>Gets the lock id.</summary>
    public Guid? LockId => TransportMessage.LockId;

    /// <summary>Gets the queue name.</summary>
    public string QueueName => TransportMessage.QueueName;
    /// <summary>Gets the priority.</summary>
    public short Priority => TransportMessage.Priority;
    /// <summary>Gets the delivery message id.</summary>
    public long DeliveryMessageId => TransportMessage.MessageDeliveryId;
    /// <summary>Gets the enqueue time.</summary>
    public DateTimeOffset EnqueueTime => TransportMessage.EnqueueTime;
    /// <summary>Gets the delivery count.</summary>
    public int DeliveryCount => TransportMessage.DeliveryCount;

    /// <summary>Gets the partition key.</summary>
    public string? PartitionKey => TransportMessage.PartitionKey;

    /// <summary>Gets transport properties.</summary>
    /// <returns>The transport properties.</returns>
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
