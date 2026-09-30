using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Combines a typed send context with RabbitMQ exchange and AMQP property state.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public class RabbitMqMessageSendContext<T> :
    MessageSendContext<T>,
    RabbitMqSendContext<T>,
    ITransportSendMetadata
    where T : class
{
    /// <summary>Creates a send context that awaits acknowledgement and uses an empty routing key by default.</summary>
    /// <param name="basicProperties">The mutable AMQP message properties.</param>
    /// <param name="exchange">The destination exchange.</param>
    /// <param name="message">The message being sent.</param>
    /// <param name="cancellationToken">Cancellation for the send pipeline.</param>
    public RabbitMqMessageSendContext(BasicProperties basicProperties, string exchange, T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
        BasicProperties = basicProperties ?? throw new ArgumentNullException(nameof(basicProperties));

        AwaitAck = true;

        RoutingKey = "";

        Exchange = exchange;
    }

    /// <summary>Gets the destination exchange, which persisted transport properties may replace internally.</summary>
    public string Exchange { get; private set; }
    /// <summary>Gets or sets the publish routing key.</summary>
    public string? RoutingKey { get; set; }
    /// <summary>Gets the mutable AMQP message properties.</summary>
    public BasicProperties BasicProperties { get; }
    /// <summary>Gets or sets whether the caller awaits the RabbitMQ client publish outcome.</summary>
    public bool AwaitAck { get; set; }

    object ITransportSendMetadata.CaptureNativeMetadata() =>
        new NativeMetadata(Exchange, RoutingKey, Durable, Mandatory, AwaitAck, Delay);

    string? ITransportSendMetadata.ChangedNativeField(object snapshot)
    {
        var expected = (NativeMetadata)snapshot;
        if (!string.Equals(Exchange, expected.Exchange, StringComparison.Ordinal)) return nameof(Exchange);
        if (!string.Equals(RoutingKey, expected.RoutingKey, StringComparison.Ordinal)) return nameof(RoutingKey);
        if (Durable != expected.Durable) return nameof(Durable);
        if (Mandatory != expected.Mandatory) return nameof(Mandatory);
        if (AwaitAck != expected.AwaitAck) return nameof(AwaitAck);
        if (Delay != expected.Delay) return nameof(Delay);
        return null;
    }

    void ITransportSendMetadata.RestoreNativeMetadata(object snapshot)
    {
        var expected = (NativeMetadata)snapshot;
        Exchange = expected.Exchange;
        RoutingKey = expected.RoutingKey;
        Durable = expected.Durable;
        Mandatory = expected.Mandatory;
        AwaitAck = expected.AwaitAck;
        Delay = expected.Delay;
    }

    private readonly record struct NativeMetadata(
        string Exchange,
        string? RoutingKey,
        bool Durable,
        bool Mandatory,
        bool AwaitAck,
        TimeSpan? Delay);

    /// <summary>Restores RabbitMQ exchange, routing, and AMQP properties from persisted transport properties.</summary>
    /// <param name="properties">The persisted transport-property bag.</param>
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

    /// <summary>Writes nonempty RabbitMQ exchange, routing, and AMQP properties for persistence.</summary>
    /// <param name="properties">The transport-property bag to update.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        properties[RabbitMqTransportPropertyNames.Exchange] = Exchange;

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[RabbitMqTransportPropertyNames.RoutingKey] = RoutingKey;

        if (BasicProperties.IsAppIdPresent() && !string.IsNullOrWhiteSpace(BasicProperties.AppId))
            properties[RabbitMqTransportPropertyNames.AppId] = BasicProperties.AppId;
        if (BasicProperties.IsPriorityPresent())
            properties[RabbitMqTransportPropertyNames.Priority] = BasicProperties.Priority;
        if (BasicProperties.IsReplyToPresent() && !string.IsNullOrWhiteSpace(BasicProperties.ReplyTo))
            properties[RabbitMqTransportPropertyNames.ReplyTo] = BasicProperties.ReplyTo;
        if (BasicProperties.IsTypePresent() && !string.IsNullOrWhiteSpace(BasicProperties.Type))
            properties[RabbitMqTransportPropertyNames.Type] = BasicProperties.Type;
        if (BasicProperties.IsUserIdPresent() && !string.IsNullOrWhiteSpace(BasicProperties.UserId))
            properties[RabbitMqTransportPropertyNames.UserId] = BasicProperties.UserId;
    }
}
