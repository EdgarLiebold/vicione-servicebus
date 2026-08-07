// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework
{
    using System;


    public interface ActivityTestContextConfigurator
    {
        void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configure);
    }
}
