using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints;

public interface ICheckpointer :
    IAsyncDisposable
{
    Task Pending(IPendingConfirmation confirmation);
}
