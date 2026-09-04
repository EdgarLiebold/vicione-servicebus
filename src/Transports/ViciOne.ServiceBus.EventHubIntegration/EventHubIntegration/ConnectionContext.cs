using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface ConnectionContext :
    PipeContext
{
    EventHubProducerClient CreateEventHubClient(string eventHubName);
}
