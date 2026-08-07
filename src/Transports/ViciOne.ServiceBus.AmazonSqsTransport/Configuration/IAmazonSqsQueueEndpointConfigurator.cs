// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface IAmazonSqsQueueEndpointConfigurator :
    IAmazonSqsQueueConfigurator
{
    ushort WaitTimeSeconds { set; }

    bool PurgeOnStartup { set; }
}
