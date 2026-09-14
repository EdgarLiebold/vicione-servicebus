using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Exposes infrastructure-level bus registration operations used by transport and provider integrations.
/// Application code should normally configure the bus through <see cref="IBusRegistrationConfigurator" />.
/// </summary>
public interface IAdvancedBusRegistrationConfigurator :
    IAdvancedRegistrationConfigurator
{
    /// <summary>Sets the transport-specific bus factory.</summary>
    /// <typeparam name="T">The transport registration factory.</typeparam>
    /// <param name="busFactory">The factory that creates the bus and its transport endpoints.</param>
    void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory;

    /// <summary>Adds a rider capability to the bus.</summary>
    /// <param name="configure">The callback that configures rider services and its runtime factory.</param>
    void AddRider(Action<IRiderRegistrationConfigurator> configure);

    /// <summary>Adds a callback invoked for each receive endpoint.</summary>
    /// <param name="callback">The callback that receives each endpoint name and configurator.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback);

    /// <summary>Adds a service-provider-aware callback invoked for each receive endpoint.</summary>
    /// <param name="callback">The callback that receives the registration context, endpoint name, and configurator.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback);

    /// <summary>Replaces the request client factory used by the bus registration.</summary>
    /// <param name="clientFactory">The factory that creates request clients for the configured bus and default timeout.</param>
    void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory);

}

/// <summary>Exposes infrastructure-level registration operations for an additional bus instance.</summary>
/// <typeparam name="TBus">The application-facing bus contract.</typeparam>
public interface IAdvancedBusRegistrationConfigurator<in TBus> :
    IAdvancedBusRegistrationConfigurator
    where TBus : class, IBus
{
    /// <summary>Adds a rider bound to the additional bus instance.</summary>
    /// <param name="configure">The callback that configures rider services bound to <typeparamref name="TBus" />.</param>
    void AddRider(Action<IRiderRegistrationConfigurator<TBus>> configure);
}
