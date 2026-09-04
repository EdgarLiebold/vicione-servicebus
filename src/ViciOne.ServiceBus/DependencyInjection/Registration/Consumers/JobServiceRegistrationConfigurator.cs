using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a job service registration configurator implementation.
/// </summary>
public class JobServiceRegistrationConfigurator :
    IJobServiceRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IJobServiceRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="registration">The registration value.</param>
    public JobServiceRegistrationConfigurator(IBusRegistrationConfigurator configurator, IJobServiceRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IJobServiceRegistrationConfigurator Options(Action<JobConsumerOptions> configure)
    {
        _registration.AddConfigureAction(configure);

        return this;
    }

    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        configure?.Invoke(_registration.EndpointRegistrationConfigurator);
    }
}
