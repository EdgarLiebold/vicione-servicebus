using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;


namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Extension surface used by persistence and transport provider packages.</summary>
public interface IReliableMessagingProviderConfigurator
{
    /// <summary>Gets the bus type.</summary>
    Type BusType { get; }

    /// <summary>Gets the services.</summary>
    IServiceCollection Services { get; }

    /// <summary>Gets the registration configurator.</summary>
    IBusRegistrationConfigurator RegistrationConfigurator { get; }

    /// <summary>Configures store for the current pipeline.</summary>
    /// <param name="implementationType">The runtime implementation type used by the operation.</param>
    void UseStore(Type implementationType);

    /// <summary>Configures dispatcher for the current pipeline.</summary>
    /// <param name="implementationType">The runtime implementation type used by the operation.</param>
    void UseDispatcher(Type implementationType);

    /// <summary>Selects the transport-native scheduling adapter for this reliable-messaging block.</summary>
    void UseTransportSchedulerAdapter();

    /// <summary>Selects an explicitly addressed scheduling adapter for this reliable-messaging block.</summary>
    /// <param name="endpointAddress">The endpoint address.</param>
    void UseEndpointSchedulerAdapter(Uri endpointAddress);
}
