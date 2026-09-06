namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines transport header names for RabbitMQ delivery metadata.</summary>
public static class RabbitMqHeaders
{
    /// <summary>The source exchange header.</summary>
    public const string Exchange = "RabbitMQ-ExchangeName";
    /// <summary>The delivery routing-key header.</summary>
    public const string RoutingKey = "RabbitMQ-RoutingKey";
    /// <summary>The channel-scoped delivery-tag header.</summary>
    public const string DeliveryTag = "RabbitMQ-DeliveryTag";
    /// <summary>The broker-assigned consumer-tag header.</summary>
    public const string ConsumerTag = "RabbitMQ-ConsumerTag";
    /// <summary>The original routing-key header used by queue redelivery.</summary>
    public const string RedeliveryRoutingKey = "RabbitMQ-Redelivery-RoutingKey";
}
