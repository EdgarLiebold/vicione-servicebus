using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints;

public interface ICheckpointer :
    IAsyncDisposable
{
    Task PendingAsync(IPendingConfirmation confirmation, CancellationToken cancellationToken = default);
}
