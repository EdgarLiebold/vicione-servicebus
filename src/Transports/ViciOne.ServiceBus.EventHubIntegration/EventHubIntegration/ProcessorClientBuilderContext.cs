using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface ProcessorClientBuilderContext
{
    Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default);
    Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default);
}
