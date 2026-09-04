using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql message send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SqlMessageSendContext<T> :
    MessageSendContext<T>,
    SqlSendContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SqlMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
        TransportMessageId = NewId.NextGuid();
    }

    /// <summary>
    /// Gets the transport message id value.
    /// </summary>
    public Guid TransportMessageId { get; }

    /// <summary>
    /// Gets or sets the partition key value.
    /// </summary>
    public string? PartitionKey { get; set; }
    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public short? Priority { get; set; }
    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        PartitionKey = ReadString(properties, SqlTransportPropertyNames.PartitionKey);
        Priority = ReadShort(properties, SqlTransportPropertyNames.Priority);
        RoutingKey = ReadString(properties, SqlTransportPropertyNames.RoutingKey);
    }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
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
