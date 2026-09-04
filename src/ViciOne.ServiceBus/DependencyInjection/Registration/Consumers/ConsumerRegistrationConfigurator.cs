using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a consumer registration configurator implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ConsumerRegistrationConfigurator<TConsumer> :
    IConsumerRegistrationConfigurator<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly IRegistrationConfigurator _configurator;
    readonly IConsumerRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="registration">The registration value.</param>
    public ConsumerRegistrationConfigurator(IRegistrationConfigurator configurator, IConsumerRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException("Consumer is excluded from ConfigureEndpoints");

        var configurator = new EndpointRegistrationConfigurator<TConsumer>();

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<ConsumerEndpointDefinition<TConsumer>, TConsumer>(_registration, configurator.Settings);
    }

    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
