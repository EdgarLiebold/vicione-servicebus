// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection.Registration
{
    using System;
    using Configuration;


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
}
