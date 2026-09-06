using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures future registration.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public class FutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    readonly IRegistrationConfigurator _configurator;
    readonly IFutureRegistration _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public FutureRegistrationConfigurator(IRegistrationConfigurator configurator, IFutureRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    IFutureRegistrationConfigurator IFutureRegistrationConfigurator.Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        return Endpoint(configure);
    }

    /// <summary>Excludes from configure endpoints.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }

    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Future Registration", "unknown", "Feature is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<TFuture>();

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<FutureEndpointDefinition<TFuture>, TFuture>(_registration, configurator.Settings);

        return this;
    }

    /// <summary>Applies the repository configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure)
    {
        var configurator = new SagaRepositoryRegistrationConfigurator<FutureState>(_configurator.Services);

        configure?.Invoke(configurator);

        return this;
    }
}
