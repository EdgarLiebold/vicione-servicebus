using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Extension surface used by persistence and transport provider packages.</summary>
public interface IReliableMessagingProviderConfigurator
{
    /// <summary>Gets the closed bus type that owns the provider registration.</summary>
    Type BusType { get; }

    /// <summary>Gets the owning dependency-injection service collection.</summary>
    IServiceCollection Services { get; }

    /// <summary>Gets the registration configurator for the owning bus.</summary>
    IBusRegistrationConfigurator RegistrationConfigurator { get; }

    /// <summary>Selects the single type implementing the outbox, inbox and schedule persistence SPIs.</summary>
    /// <param name="implementationType">The closed store implementation type.</param>
    void UseStore(Type implementationType);

    /// <summary>Selects the single transport dispatcher for the owning bus.</summary>
    /// <param name="implementationType">The closed dispatcher implementation type.</param>
    void UseDispatcher(Type implementationType);

    /// <summary>Selects the transport-native scheduling adapter for this reliable-messaging block.</summary>
    void UseTransportSchedulerAdapter();

    /// <summary>Selects an explicitly addressed scheduling adapter for this reliable-messaging block.</summary>
    /// <param name="endpointAddress">The absolute scheduler endpoint address.</param>
    void UseEndpointSchedulerAdapter(Uri endpointAddress);
}
