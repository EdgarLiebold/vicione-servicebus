using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface ProcessorClientBuilderContext
{
    Task OnPartitionInitializing(PartitionInitializingEventArgs eventArgs);
    Task OnPartitionClosing(PartitionClosingEventArgs eventArgs);
}
