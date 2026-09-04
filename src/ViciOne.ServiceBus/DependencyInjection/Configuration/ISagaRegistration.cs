using System;

namespace ViciOne.ServiceBus.Configuration;

public interface ISagaRegistration :
    IRegistration
{
    void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
        where T : class, ISaga;

    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    ISagaDefinition GetDefinition(IRegistrationContext context);
}
