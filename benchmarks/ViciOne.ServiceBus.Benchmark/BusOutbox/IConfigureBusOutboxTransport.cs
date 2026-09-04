using System;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.BusOutbox;

public interface IConfigureBusOutboxTransport
{
    void Using(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> callback);
}
