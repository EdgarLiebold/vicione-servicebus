
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about consumer configuration events.</summary>
public interface IConsumerConfigurationObserver
{
    /// <summary>Called when a consumer is configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class;

    /// <summary>Called when a consumer/message combination is configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class;
}
