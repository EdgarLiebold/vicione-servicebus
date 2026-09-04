using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public interface IBusOutboxNotification<TScope>
    where TScope : class
{
    Task WaitForDelivery(CancellationToken cancellationToken);
    void Delivered();
}
