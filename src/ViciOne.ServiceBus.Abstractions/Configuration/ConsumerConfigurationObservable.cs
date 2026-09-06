using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for consumer configuration.</summary>
public class ConsumerConfigurationObservable :
    Connectable<IConsumerConfigurationObserver>,
    IConsumerConfigurationObserver
{
    /// <summary>Consumes r configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerConfigured(configurator));
    }

    /// <summary>Consumes r message configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerMessageConfigured(configurator));
    }
}
