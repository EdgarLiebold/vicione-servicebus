using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints;

public class PendingConfirmation :
    IPendingConfirmation
{
    readonly TaskCompletionSource<string> _source;
    ProcessEventArgs _eventArgs;

    public PendingConfirmation(ProcessEventArgs eventArgs)
    {
        _eventArgs = eventArgs;
        _source = TaskCompletionSources.Create<string>();
    }

    Uri Topic => new Uri($"topic:{Partition.EventHubName}");

    public PartitionContext Partition => _eventArgs.Partition;

    public string OffsetString => _eventArgs.Data.OffsetString;

    public Task Confirmed => _source.Task;

    public void Complete()
    {
        _source.TrySetResult(OffsetString);
    }

    public void Faulted(Exception exception)
    {
        _source.TrySetException(new MessageNotConsumedException(Topic, "Message not consumed", exception));
    }

    public void Faulted(string message)
    {
        _source.TrySetException(new ArgumentException(message));
    }

    public void Canceled(CancellationToken cancellationToken)
    {
        _source.TrySetCanceled(cancellationToken);
    }

    public Task CheckpointAsync(CancellationToken cancellationToken)
    {
        return _eventArgs.UpdateCheckpointAsync(cancellationToken);
    }
}
