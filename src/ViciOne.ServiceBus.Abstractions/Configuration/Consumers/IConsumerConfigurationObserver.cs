
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer configuration observer.
/// </summary>
public interface IConsumerConfigurationObserver
{
    /// <summary>
    /// Called when a consumer is configured
    /// </summary>
    /// <typeparam name="TConsumer"></typeparam>
    /// <param name="configurator"></param>
    void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class;

    /// <summary>
    /// Called when a consumer/message combination is configured
    /// </summary>
    /// <typeparam name="TConsumer"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="configurator"></param>
    void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class;
}
