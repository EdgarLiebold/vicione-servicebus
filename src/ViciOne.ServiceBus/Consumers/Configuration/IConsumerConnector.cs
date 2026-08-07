// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// Interface implemented by objects that tie an inbound pipeline together with
    /// consumers (by means of calling a consumer factory).
    /// </summary>
    public interface IConsumerConnector
    {
        IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
            where TConsumer : class;

        ConnectHandle ConnectConsumer<TConsumer>(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
            IConsumerSpecification<TConsumer> specification)
            where TConsumer : class;
    }
}
