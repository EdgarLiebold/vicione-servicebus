using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a future registration configurator implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public class FutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    readonly IRegistrationConfigurator _configurator;
    readonly IFutureRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="registration">The registration value.</param>
    public FutureRegistrationConfigurator(IRegistrationConfigurator configurator, IFutureRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    IFutureRegistrationConfigurator IFutureRegistrationConfigurator.Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        return Endpoint(configure);
    }

    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }

    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Future Registration", "unknown", "Feature is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<TFuture>();

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<FutureEndpointDefinition<TFuture>, TFuture>(_registration, configurator.Settings);

        return this;
    }

    /// <summary>
    /// Performs the repository operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure)
    {
        var configurator = new SagaRepositoryRegistrationConfigurator<FutureState>(_configurator.Services);

        configure?.Invoke(configurator);

        return this;
    }
}
