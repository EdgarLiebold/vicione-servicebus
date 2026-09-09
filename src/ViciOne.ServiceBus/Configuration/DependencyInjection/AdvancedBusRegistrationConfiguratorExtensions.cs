using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides opt-in access to infrastructure-level bus registration operations.</summary>
public static class AdvancedBusRegistrationConfiguratorExtensions
{
    /// <summary>Selects the advanced configuration API.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The advanced registration configurator produced by the operation.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedRegistrationConfigurator Advanced(this IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedRegistrationConfigurator
            ?? throw Unsupported(configurator);
    }

    /// <summary>Selects the advanced configuration API.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The advanced bus registration configurator produced by the operation.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedBusRegistrationConfigurator Advanced(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedBusRegistrationConfigurator
            ?? throw Unsupported(configurator);
    }

    /// <summary>Selects the advanced configuration API.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="configurator">The typed application bus registration configurator.</param>
    /// <returns>The typed advanced bus registration contract.</returns>
    /// <exception cref="ArgumentNullException">The configurator is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The configurator does not support advanced registration.</exception>
    public static IAdvancedBusRegistrationConfigurator<TBus> Advanced<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator as IAdvancedBusRegistrationConfigurator<TBus>
            ?? throw Unsupported(configurator);
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

    /// <inheritdoc cref="IAdvancedRegistrationConfigurator.GetOrAddRegistrationCompletionParticipant{TParticipant}" />
    public static TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(this IRegistrationConfigurator configurator,
        Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant =>
        configurator.Advanced().GetOrAddRegistrationCompletionParticipant(factory);

    static ConfigurationException Unsupported(object configurator) =>
        new(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
            "Advanced Bus Registration Configurator Extensions",
            "unknown",
            $"The registration configurator '{configurator.GetType().FullName}' does not support advanced registration.",
            "Correct the named configuration before starting the host"));
}
