// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Processor;


    public interface IProcessorLockContext :
        IAsyncDisposable
    {
        Task Pending(ProcessEventArgs eventArgs);
        Task Complete(ProcessEventArgs eventArgs);
        Task Faulted(ProcessEventArgs eventArgs, Exception exception);
        void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken);
    }
}
