namespace ViciOneServiceBusBenchmark.BusOutbox;

using System;
using ViciOne.ServiceBus;


public interface IConfigureBusOutboxTransport
{
    void Using(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> callback);
}
