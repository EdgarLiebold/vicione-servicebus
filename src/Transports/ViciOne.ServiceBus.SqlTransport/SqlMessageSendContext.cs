using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for sql message send operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SqlMessageSendContext<T> :
    MessageSendContext<T>,
    SqlSendContext<T>
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SqlMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
        TransportMessageId = NewId.NextGuid();
    }

    /// <summary>Gets the transport message id.</summary>
    public Guid TransportMessageId { get; }

    /// <summary>Gets or sets the partition key.</summary>
    public string? PartitionKey { get; set; }
    /// <summary>Gets or sets the priority.</summary>
    public short? Priority { get; set; }
    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    /// <summary>Reads properties from.</summary>
    /// <param name="properties">The properties.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        PartitionKey = ReadString(properties, SqlTransportPropertyNames.PartitionKey);
        Priority = ReadShort(properties, SqlTransportPropertyNames.Priority);
        RoutingKey = ReadString(properties, SqlTransportPropertyNames.RoutingKey);
    }

    /// <summary>Writes properties to.</summary>
    /// <param name="properties">The properties.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(PartitionKey))
            properties[SqlTransportPropertyNames.PartitionKey] = PartitionKey!;
        if (Priority.HasValue)
            properties[SqlTransportPropertyNames.Priority] = Priority.Value;
        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[SqlTransportPropertyNames.RoutingKey] = RoutingKey!;
    }
}
