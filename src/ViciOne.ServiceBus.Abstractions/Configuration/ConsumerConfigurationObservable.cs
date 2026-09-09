using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for consumer configuration.</summary>
public class ConsumerConfigurationObservable :
    Connectable<IConsumerConfigurationObserver>,
    IConsumerConfigurationObserver
{
    /// <summary>Reports that a consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The configured consumer implementation.</typeparam>
    /// <param name="configurator">The completed consumer configuration.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerConfigured(configurator));
    }

    /// <summary>Reports that a consumer's message pipeline has been configured.</summary>
    /// <typeparam name="TConsumer">The configured consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The configured message contract.</typeparam>
    /// <param name="configurator">The completed consumer-message configuration.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ConsumerMessageConfigured(configurator));
    }
}
