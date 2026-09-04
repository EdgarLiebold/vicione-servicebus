using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class HandlerConfigurationObservable :
    Connectable<IHandlerConfigurationObserver>,
    IHandlerConfigurationObserver
{
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.HandlerConfigured(configurator));
    }
}
