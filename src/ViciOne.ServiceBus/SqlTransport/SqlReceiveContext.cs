using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql receive context implementation.
/// </summary>
public sealed class SqlReceiveContext :
    BaseReceiveContext,
    SqlMessageContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;
    IHeaderProvider? _headerProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="clientContext">The client context value.</param>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="lockContext">The lock context value.</param>
    public SqlReceiveContext(SqlTransportMessage message, SqlReceiveEndpointContext context, ReceiveSettings settings, ClientContext clientContext,
        ConnectionContext connectionContext, SqlReceiveLockContext lockContext)
        : base(message.DeliveryCount > 0, context, settings, clientContext, connectionContext, lockContext)
    {
        TransportMessage = message;

        _body = message.Body != null
            ? new StringMessageBody(message.Body)
            : new BytesMessageBody(message.BinaryBody);
    }

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => _headerProvider ??= new SqlHeaderProvider(TransportMessage);

    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    public ulong? SequenceNumber => (ulong)DeliveryMessageId;

    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    public SqlTransportMessage TransportMessage { get; }

    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string? RoutingKey => TransportMessage.RoutingKey;
    /// <summary>
    /// Gets the transport message id value.
    /// </summary>
    public Guid TransportMessageId => TransportMessage.TransportMessageId;

    /// <summary>
    /// Gets the consumer id value.
    /// </summary>
    public Guid? ConsumerId => TransportMessage.ConsumerId;
    /// <summary>
    /// Gets the lock id value.
    /// </summary>
    public Guid? LockId => TransportMessage.LockId;

    /// <summary>
    /// Gets the queue name value.
    /// </summary>
    public string QueueName => TransportMessage.QueueName;
    /// <summary>
    /// Gets the priority value.
    /// </summary>
    public short Priority => TransportMessage.Priority;
    /// <summary>
    /// Gets the delivery message id value.
    /// </summary>
    public long DeliveryMessageId => TransportMessage.MessageDeliveryId;
    /// <summary>
    /// Gets the enqueue time value.
    /// </summary>
    public DateTimeOffset EnqueueTime => TransportMessage.EnqueueTime;
    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    public int DeliveryCount => TransportMessage.DeliveryCount;

    /// <summary>
    /// Gets the partition key value.
    /// </summary>
    public string? PartitionKey => TransportMessage.PartitionKey;

    /// <summary>
    /// Gets transport properties.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties.Value[SqlTransportPropertyNames.RoutingKey] = RoutingKey!;

        if (!string.IsNullOrWhiteSpace(PartitionKey))
            properties.Value[SqlTransportPropertyNames.PartitionKey] = PartitionKey!;

        if (Priority != 100)
            properties.Value[SqlTransportPropertyNames.Priority] = Priority;

        return properties.IsValueCreated ? properties.Value : null;
    }
}
