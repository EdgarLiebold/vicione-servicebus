// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Azure.Messaging.EventHubs;
    using Logging;


    public interface ProcessorContext :
        PipeContext
    {
        ILogContext LogContext { get; }
        EventProcessorClient GetClient(ProcessorClientBuilderContext context);
        void ReleaseClient(ProcessorClientBuilderContext processorLockContext);
    }
}
