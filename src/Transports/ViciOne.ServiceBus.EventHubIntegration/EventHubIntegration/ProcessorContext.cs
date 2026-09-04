using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface ProcessorContext :
    PipeContext
{
    ILogContext LogContext { get; }
    EventProcessorClient GetClient(ProcessorClientBuilderContext context);
    void ReleaseClient(ProcessorClientBuilderContext processorLockContext);
}
