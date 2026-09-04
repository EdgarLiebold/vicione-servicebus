using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public interface IBusOutboxNotification<TScope>
    where TScope : class
{
    Task WaitForDeliveryAsync(CancellationToken cancellationToken);
    void Delivered();
}
