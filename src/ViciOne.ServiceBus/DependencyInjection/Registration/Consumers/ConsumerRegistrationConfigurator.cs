using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures consumer registration.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerRegistrationConfigurator<TConsumer> :
    IConsumerRegistrationConfigurator<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly IRegistrationConfigurator _configurator;
    readonly IConsumerRegistration _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public ConsumerRegistrationConfigurator(IRegistrationConfigurator configurator, IConsumerRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer Registration", "unknown", "Consumer is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<TConsumer>();

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<ConsumerEndpointDefinition<TConsumer>, TConsumer>(_registration, configurator.Settings);
    }

    /// <summary>Excludes from configure endpoints.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
