// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Processor;


    public interface ProcessorClientBuilderContext
    {
        Task OnPartitionInitializing(PartitionInitializingEventArgs eventArgs);
        Task OnPartitionClosing(PartitionClosingEventArgs eventArgs);
    }
}
