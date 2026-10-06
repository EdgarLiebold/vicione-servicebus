using System;

namespace ViciOne.ServiceBus.Configuration;

// A callback belongs to one endpoint invocation, never to registration metadata.
internal interface IEndpointSagaRegistration<TSaga>
    where TSaga : class
{
    void Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context,
        Action<ISagaConfigurator<TSaga>> configure);
}
