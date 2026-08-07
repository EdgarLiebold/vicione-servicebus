// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IConsumerRegistrationConfigurator<TConsumer> :
        IConsumerRegistrationConfigurator
        where TConsumer : class, IConsumer
    {
    }


    public interface IConsumerRegistrationConfigurator
    {
        void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
        void ExcludeFromConfigureEndpoints();
    }
}
