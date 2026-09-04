using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer configuration observable implementation.
/// </summary>
public class ConsumerConfigurationObservable :
    Connectable<IConsumerConfigurationObserver>,
    IConsumerConfigurationObserver
{
    /// <summary>
    /// Consumes r configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerConfigured(configurator));
    }

    /// <summary>
    /// Consumes r message configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerMessageConfigured(configurator));
    }
}
