using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures job service registration.</summary>
public class JobServiceRegistrationConfigurator :
    IJobServiceRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IJobServiceRegistration _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public JobServiceRegistrationConfigurator(IBusRegistrationConfigurator configurator, IJobServiceRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>Applies the configured options.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job service registration configurator produced by the operation.</returns>
    public IJobServiceRegistrationConfigurator Options(Action<JobConsumerOptions> configure)
    {
        _registration.AddConfigureAction(configure);

        return this;
    }

    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        configure?.Invoke(_registration.EndpointRegistrationConfigurator);
    }
}
