using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for handler configuration.</summary>
public class HandlerConfigurationObservable :
    Connectable<IHandlerConfigurationObserver>,
    IHandlerConfigurationObserver
{
    /// <summary>Reports that handler has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.HandlerConfigured(configurator));
    }
}
