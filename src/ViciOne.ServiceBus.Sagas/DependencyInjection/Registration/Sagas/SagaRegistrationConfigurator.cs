using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures saga registration.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaRegistrationConfigurator<TSaga> :
    ISagaRegistrationConfigurator<TSaga>
    where TSaga : class, ISaga
{
    readonly IRegistrationConfigurator _configurator;
    readonly ISagaRegistration? _registration = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public SagaRegistrationConfigurator(IRegistrationConfigurator configurator, ISagaRegistration? registration = null)
    {
        _configurator = configurator;
        _registration = registration;
    }

    ISagaRegistrationConfigurator ISagaRegistrationConfigurator.Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        return Endpoint(configure);
    }

    void ISagaRegistrationConfigurator.ExcludeFromConfigureEndpoints()
    {
        if (_registration != null)
            _registration.IncludeInConfigureEndpoints = false;
    }

    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<TSaga> Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (_registration is { IncludeInConfigureEndpoints: false })
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "Saga is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<TSaga>();

        configure?.Invoke(configurator);

        var registration = _registration
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "An endpoint cannot be configured for a repository-only saga registration.", "Correct the named configuration before starting the host"));
        _configurator.AddEndpoint<SagaEndpointDefinition<TSaga>, TSaga>(registration, configurator.Settings);

        return this;
    }

    /// <summary>Applies the repository configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<TSaga> Repository(Action<ISagaRepositoryRegistrationConfigurator<TSaga>> configure)
    {
        var configurator = new SagaRepositoryRegistrationConfigurator<TSaga>(_configurator.Services);

        configure?.Invoke(configurator);

        return this;
    }
}
