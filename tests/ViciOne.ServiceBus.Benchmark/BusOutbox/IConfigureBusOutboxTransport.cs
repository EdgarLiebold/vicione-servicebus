// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark.BusOutbox;

using System;
using ViciOne.ServiceBus;


public interface IConfigureBusOutboxTransport
{
    void Using(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> callback);
}
