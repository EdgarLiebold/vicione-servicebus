// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware.Outbox
{
    using System.Threading;
    using System.Threading.Tasks;


    public interface IBusOutboxNotification
    {
        Task WaitForDelivery(CancellationToken cancellationToken);
        void Delivered();
    }
}
