using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Exposes infrastructure-level bus registration operations used by transport and provider integrations.
/// Application code should normally configure the bus through <see cref="IBusRegistrationConfigurator" />.
/// </summary>
public interface IAdvancedBusRegistrationConfigurator :
    IAdvancedRegistrationConfigurator
{
    /// <summary>
    /// Sets the transport-specific bus factory.
    /// </summary>
    /// <typeparam name="T">The registration bus factory type.</typeparam>
    /// <param name="busFactory">The bus factory.</param>
    void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory;

    /// <summary>
    /// Adds a bus rider.
    /// </summary>
    /// <param name="configure">The rider registration callback.</param>
    void AddRider(Action<IRiderRegistrationConfigurator> configure);

    /// <summary>
    /// Adds a callback invoked for each receive endpoint.
    /// </summary>
    /// <param name="callback">The endpoint callback.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback);

    /// <summary>
    /// Adds a service-provider-aware callback invoked for each receive endpoint.
    /// </summary>
    /// <param name="callback">The endpoint callback.</param>
    void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback);

    /// <summary>
    /// Replaces the request client factory used by the bus registration.
    /// </summary>
    /// <param name="clientFactory">The request client factory.</param>
    void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory);

}

/// <summary>Exposes infrastructure-level registration operations for an additional bus instance.</summary>
public interface IAdvancedBusRegistrationConfigurator<in TBus> :
    IAdvancedBusRegistrationConfigurator
    where TBus : class, IBus
{
    /// <summary>
    /// Adds a rider bound to the additional bus instance.
    /// </summary>
    /// <param name="configure">The rider registration callback.</param>
    void AddRider(Action<IRiderRegistrationConfigurator<TBus>> configure);
}
