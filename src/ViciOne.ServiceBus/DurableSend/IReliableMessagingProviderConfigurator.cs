using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Extension surface used by persistence and transport provider packages.</summary>
public interface IReliableMessagingProviderConfigurator
{
    /// <summary>
    /// Gets the bus type value.
    /// </summary>
    Type BusType { get; }

    /// <summary>
    /// Gets the services value.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the registration configurator that owns this reliable-messaging block.
    /// </summary>
    IBusRegistrationConfigurator RegistrationConfigurator { get; }

    /// <summary>
    /// Configures store for the current pipeline.
    /// </summary>
    /// <param name="implementationType">The implementation type value.</param>
    void UseStore(Type implementationType);

    /// <summary>
    /// Configures dispatcher for the current pipeline.
    /// </summary>
    /// <param name="implementationType">The implementation type value.</param>
    void UseDispatcher(Type implementationType);

    /// <summary>
    /// Selects the transport-native scheduling adapter for this reliable-messaging block.
    /// </summary>
    void UseTransportSchedulerAdapter();

    /// <summary>
    /// Selects an explicitly addressed scheduling adapter for this reliable-messaging block.
    /// </summary>
    /// <param name="endpointAddress">The scheduler endpoint address.</param>
    void UseEndpointSchedulerAdapter(Uri endpointAddress);
}
