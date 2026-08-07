// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IMessageConnectorFactory
    {
        IConsumerMessageConnector<T> CreateConsumerConnector<T>()
            where T : class;

        IInstanceMessageConnector<T> CreateInstanceConnector<T>()
            where T : class;
    }
}
