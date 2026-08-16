namespace ViciOne.ServiceBus;

public interface IAmazonSqsQueueEndpointConfigurator :
    IAmazonSqsQueueConfigurator
{
    ushort WaitTimeSeconds { set; }

    bool PurgeOnStartup { set; }
}
