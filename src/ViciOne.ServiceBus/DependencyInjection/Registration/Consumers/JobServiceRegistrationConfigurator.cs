using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

public class JobServiceRegistrationConfigurator :
    IJobServiceRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IJobServiceRegistration _registration;

    public JobServiceRegistrationConfigurator(IBusRegistrationConfigurator configurator, IJobServiceRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    public IJobServiceRegistrationConfigurator Options(Action<JobConsumerOptions> configure)
    {
        _registration.AddConfigureAction(configure);

        return this;
    }

    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        configure?.Invoke(_registration.EndpointRegistrationConfigurator);
    }
}
