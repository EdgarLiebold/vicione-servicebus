// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints
{
    using System;
    using System.Threading.Tasks;


    public interface ICheckpointer :
        IAsyncDisposable
    {
        Task Pending(IPendingConfirmation confirmation);
    }
}
