using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq message send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RabbitMqMessageSendContext<T> :
    MessageSendContext<T>,
    RabbitMqSendContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="basicProperties">The basic properties value.</param>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public RabbitMqMessageSendContext(BasicProperties basicProperties, string exchange, T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
        BasicProperties = basicProperties;

        AwaitAck = true;

        RoutingKey = "";

        Exchange = exchange;
    }

    /// <summary>
    /// Gets or sets the exchange value.
    /// </summary>
    public string Exchange { get; private set; }
    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }
    /// <summary>
    /// Gets the basic properties value.
    /// </summary>
    public BasicProperties BasicProperties { get; }
    /// <summary>
    /// Gets or sets the await ack value.
    /// </summary>
    public bool AwaitAck { get; set; }

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        Exchange = ReadString(properties, RabbitMqTransportPropertyNames.Exchange, Exchange) ?? Exchange;
        RoutingKey = ReadString(properties, RabbitMqTransportPropertyNames.RoutingKey, "") ?? "";

        BasicProperties.AppId = ReadString(properties, RabbitMqTransportPropertyNames.AppId);
        BasicProperties.Priority = ReadByte(properties, RabbitMqTransportPropertyNames.Priority);
        BasicProperties.ReplyTo = ReadString(properties, RabbitMqTransportPropertyNames.ReplyTo);
        BasicProperties.Type = ReadString(properties, RabbitMqTransportPropertyNames.Type);
        BasicProperties.UserId = ReadString(properties, RabbitMqTransportPropertyNames.UserId);
    }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        properties[RabbitMqTransportPropertyNames.Exchange] = Exchange;

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[RabbitMqTransportPropertyNames.RoutingKey] = RoutingKey;

        if (BasicProperties.IsAppIdPresent())
            properties[RabbitMqTransportPropertyNames.AppId] = BasicProperties.AppId;
        if (BasicProperties.IsPriorityPresent())
            properties[RabbitMqTransportPropertyNames.Priority] = BasicProperties.Priority;
        if (BasicProperties.IsReplyToPresent())
            properties[RabbitMqTransportPropertyNames.ReplyTo] = BasicProperties.ReplyTo;
        if (BasicProperties.IsTypePresent())
            properties[RabbitMqTransportPropertyNames.Type] = BasicProperties.Type;
        if (BasicProperties.IsUserIdPresent())
            properties[RabbitMqTransportPropertyNames.UserId] = BasicProperties.UserId;
    }
}
