using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints;

public interface IPendingConfirmation
{
    PartitionContext Partition { get; }

    string OffsetString { get; }

    Task Confirmed { get; }

    void Complete();
    void Faulted(Exception exception);
    void Faulted(string message);
    void Canceled(CancellationToken cancellationToken);

    Task CheckpointAsync(CancellationToken cancellationToken);
}
