using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Applies endpoint and repository choices to one future registration.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
internal sealed class FutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator<TFuture>
    where TFuture : class, ISagaStateMachine<FutureState>
{
    readonly IRegistrationConfigurator _configurator;
    readonly IFutureRegistration _registration;

    /// <summary>Creates a public configuration surface for an internal future registration.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="registration">The future registration to update.</param>
    public FutureRegistrationConfigurator(IRegistrationConfigurator configurator, IFutureRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(registration);
        _configurator = configurator;
        _registration = registration;
    }

    IFutureRegistrationConfigurator IFutureRegistrationConfigurator.Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        return Endpoint(configure);
    }

    /// <summary>Excludes the future from convention-based endpoint configuration.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }

    /// <summary>Adds endpoint settings to the future registration.</summary>
    /// <param name="configure">The callback that configures the future's receive endpoint.</param>
    /// <returns>The same future registration configurator.</returns>
    public IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Future Registration", "unknown", "Feature is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<TFuture>();

        configure(configurator);

        _configurator.AddEndpoint<FutureEndpointDefinition<TFuture>, TFuture>(_registration, configurator.Settings);

        return this;
    }

    /// <summary>Configures the repository that persists future state.</summary>
    /// <param name="configure">The callback that selects and configures the saga repository.</param>
    /// <returns>The same future registration configurator.</returns>
    public IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configurator = new SagaRepositoryRegistrationConfigurator<FutureState>(_configurator.Services);

        configure(configurator);

        return this;
    }
}
