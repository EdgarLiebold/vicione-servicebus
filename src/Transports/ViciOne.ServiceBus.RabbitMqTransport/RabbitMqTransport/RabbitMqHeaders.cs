// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    public static class RabbitMqHeaders
    {
        public const string Exchange = "RabbitMQ-ExchangeName";
        public const string RoutingKey = "RabbitMQ-RoutingKey";
        public const string DeliveryTag = "RabbitMQ-DeliveryTag";
        public const string ConsumerTag = "RabbitMQ-ConsumerTag";
    }
}
