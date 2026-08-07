// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public interface IMessageFabricObserver<in TContext>
        where TContext : class
    {
        void ExchangeDeclared(TContext context, string name, ExchangeType exchangeType);

        void ExchangeBindingCreated(TContext context, string source, string destination, string routingKey = default);

        void QueueDeclared(TContext context, string name);

        void QueueBindingCreated(TContext context, string source, string destination);

        TopologyHandle ConsumerConnected(TContext context, TopologyHandle handle, string queueName);
    }
}
