namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq headers implementation.
/// </summary>
public static class RabbitMqHeaders
{
    /// <summary>
    /// Defines the exchange value.
    /// </summary>
    public const string Exchange = "RabbitMQ-ExchangeName";
    /// <summary>
    /// Defines the routing key value.
    /// </summary>
    public const string RoutingKey = "RabbitMQ-RoutingKey";
    /// <summary>
    /// Defines the delivery tag value.
    /// </summary>
    public const string DeliveryTag = "RabbitMQ-DeliveryTag";
    /// <summary>
    /// Defines the consumer tag value.
    /// </summary>
    public const string ConsumerTag = "RabbitMQ-ConsumerTag";
    /// <summary>
    /// Defines the redelivery routing key value.
    /// </summary>
    public const string RedeliveryRoutingKey = "RabbitMQ-Redelivery-RoutingKey";
}
