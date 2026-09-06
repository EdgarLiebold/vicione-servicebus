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
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="busFactory">The bus factory.</param>
    void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory;

    /// <summary>Adds a bus rider.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddRider(Action<IRiderRegistrationConfigurator> configure);

    /// <summary>Adds a callback invoked for each receive endpoint.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback);

    /// <summary>Adds a service-provider-aware callback invoked for each receive endpoint.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback);

    /// <summary>Replaces the request client factory used by the bus registration.</summary>
    /// <param name="clientFactory">The client factory.</param>
    void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory);

}

/// <summary>Exposes infrastructure-level registration operations for an additional bus instance.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IAdvancedBusRegistrationConfigurator<in TBus> :
    IAdvancedBusRegistrationConfigurator
    where TBus : class, IBus
{
    /// <summary>Adds a rider bound to the additional bus instance.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddRider(Action<IRiderRegistrationConfigurator<TBus>> configure);
}
