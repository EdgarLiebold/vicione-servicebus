using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IProcessorLockContext :
    IAsyncDisposable
{
    Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default);
    void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken);
}
