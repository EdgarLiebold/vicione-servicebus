// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ISagaRegistrationConfigurator<TSaga> :
        ISagaRegistrationConfigurator
        where TSaga : class, ISaga
    {
        new ISagaRegistrationConfigurator<TSaga> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
        ISagaRegistrationConfigurator<TSaga> Repository(Action<ISagaRepositoryRegistrationConfigurator<TSaga>> configure);
    }


    public interface ISagaRegistrationConfigurator
    {
        ISagaRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
        void ExcludeFromConfigureEndpoints();
    }
}
