// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface IJobServiceRegistration :
        IRegistration
    {
        IEndpointRegistrationConfigurator EndpointRegistrationConfigurator { get; }

        IEndpointDefinition EndpointDefinition { get; }

        void AddConfigureAction(Action<JobConsumerOptions> configure);

        void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency);

        void Configure(IServiceInstanceConfigurator instanceConfigurator, IRegistrationContext context);
    }
}
