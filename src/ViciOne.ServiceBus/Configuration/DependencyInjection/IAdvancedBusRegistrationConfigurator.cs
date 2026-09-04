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
    /// Gets the container registrar used by provider integrations.
    /// </summary>
    IContainerRegistrar Registrar { get; }

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

/// <summary>
/// Exposes infrastructure-level operations shared by registration configurators.
/// </summary>
public interface IAdvancedRegistrationConfigurator
{
    /// <summary>
    /// Adds a typed endpoint definition for an existing registration.
    /// </summary>
    /// <typeparam name="TDefinition">The endpoint definition type.</typeparam>
    /// <typeparam name="T">The registered service type.</typeparam>
    /// <param name="registration">The registration to associate with the endpoint.</param>
    /// <param name="settings">Optional endpoint settings.</param>
    void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class;

    /// <summary>
    /// Sets the default request timeout from individual duration components.
    /// </summary>
    /// <param name="d">Days.</param>
    /// <param name="h">Hours.</param>
    /// <param name="m">Minutes.</param>
    /// <param name="s">Seconds.</param>
    /// <param name="ms">Milliseconds.</param>
    void SetDefaultRequestTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null);

    /// <summary>
    /// Sets the provider used to configure saga repositories registered by type.
    /// </summary>
    /// <param name="provider">The saga repository registration provider.</param>
    void SetSagaRepositoryProvider(ISagaRepositoryRegistrationProvider provider);
}

/// <summary>
/// Exposes infrastructure-level registration operations for an additional bus instance.
/// </summary>
/// <typeparam name="TBus">The additional bus interface type.</typeparam>
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

/// <summary>
/// Provides opt-in access to infrastructure-level bus registration operations.
/// </summary>
public static class AdvancedBusRegistrationConfiguratorExtensions
{
    /// <summary>
    /// Gets the advanced contract implemented by the built-in registration configurator.
    /// </summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <returns>The advanced registration contract.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedRegistrationConfigurator Advanced(this IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedRegistrationConfigurator
            ?? throw new ConfigurationException($"The registration configurator '{configurator.GetType().FullName}' does not support advanced registration.");
    }

    /// <summary>
    /// Gets the advanced registration contract implemented by the built-in configurator.
    /// </summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <returns>The advanced registration contract.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedBusRegistrationConfigurator Advanced(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedBusRegistrationConfigurator
            ?? throw new ConfigurationException($"The registration configurator '{configurator.GetType().FullName}' does not support advanced registration.");
    }

    /// <summary>
    /// Gets the advanced registration contract implemented by the built-in multi-bus configurator.
    /// </summary>
    /// <typeparam name="TBus">The additional bus interface type.</typeparam>
    /// <param name="configurator">The application registration configurator.</param>
    /// <returns>The advanced registration contract.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedBusRegistrationConfigurator<TBus> Advanced<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedBusRegistrationConfigurator<TBus>
            ?? throw new ConfigurationException($"The registration configurator '{configurator.GetType().FullName}' does not support advanced registration.");
    }

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator.SetBusFactory{T}(T)" />
    public static void SetBusFactory<T>(this IBusRegistrationConfigurator configurator, T busFactory)
        where T : class, IRegistrationBusFactory => configurator.Advanced().SetBusFactory(busFactory);

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator.AddRider(Action{IRiderRegistrationConfigurator})" />
    public static void AddRider(this IBusRegistrationConfigurator configurator, Action<IRiderRegistrationConfigurator> configure) =>
        configurator.Advanced().AddRider(configure);

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator{TBus}.AddRider(Action{IRiderRegistrationConfigurator{TBus}})" />
    public static void AddRider<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IRiderRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus => configurator.Advanced().AddRider(configure);

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator.AddConfigureEndpointsCallback(ConfigureEndpointsCallback)" />
    public static void AddConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, ConfigureEndpointsCallback callback) =>
        configurator.Advanced().AddConfigureEndpointsCallback(callback);

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator.AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback)" />
    public static void AddConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, ConfigureEndpointsProviderCallback callback) =>
        configurator.Advanced().AddConfigureEndpointsCallback(callback);

    /// <inheritdoc cref="IAdvancedBusRegistrationConfigurator.SetRequestClientFactory" />
    public static void SetRequestClientFactory(this IBusRegistrationConfigurator configurator,
        Func<IBus, RequestTimeout, IClientFactory> clientFactory) => configurator.Advanced().SetRequestClientFactory(clientFactory);

    /// <inheritdoc cref="IAdvancedRegistrationConfigurator.AddEndpoint{TDefinition,T}" />
    public static void AddEndpoint<TDefinition, T>(this IRegistrationConfigurator configurator, IRegistration registration,
        IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class => configurator.Advanced().AddEndpoint<TDefinition, T>(registration, settings);

    /// <inheritdoc cref="IAdvancedRegistrationConfigurator.SetDefaultRequestTimeout(int?,int?,int?,int?,int?)" />
    public static void SetDefaultRequestTimeout(this IRegistrationConfigurator configurator, int? d = null, int? h = null, int? m = null,
        int? s = null, int? ms = null) => configurator.Advanced().SetDefaultRequestTimeout(d, h, m, s, ms);

    /// <inheritdoc cref="IAdvancedRegistrationConfigurator.SetSagaRepositoryProvider" />
    public static void SetSagaRepositoryProvider(this IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider) =>
        configurator.Advanced().SetSagaRepositoryProvider(provider);
}
