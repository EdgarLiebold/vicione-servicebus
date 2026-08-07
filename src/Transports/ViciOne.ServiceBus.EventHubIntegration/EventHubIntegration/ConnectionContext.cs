// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Azure.Messaging.EventHubs.Producer;


    public interface ConnectionContext :
        PipeContext
    {
        EventHubProducerClient CreateEventHubClient(string eventHubName);
    }
}
