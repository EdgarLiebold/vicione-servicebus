// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using RabbitMqTransport.Topology;


    public interface IRabbitMqConsumeTopology :
        IConsumeTopology
    {
        IExchangeTypeSelector ExchangeTypeSelector { get; }

        new IRabbitMqMessageConsumeTopology<T> GetMessageTopology<T>()
            where T : class;

        /// <summary>
        /// Apply the entire topology to the builder
        /// </summary>
        /// <param name="builder"></param>
        void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
    }
}
