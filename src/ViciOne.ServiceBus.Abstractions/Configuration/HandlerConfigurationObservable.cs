using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a handler configuration observable implementation.
/// </summary>
public class HandlerConfigurationObservable :
    Connectable<IHandlerConfigurationObserver>,
    IHandlerConfigurationObserver
{
    /// <summary>
    /// Performs the handler configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.HandlerConfigured(configurator));
    }
}
