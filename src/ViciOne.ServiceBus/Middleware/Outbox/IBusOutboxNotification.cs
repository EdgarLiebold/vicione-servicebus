using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Defines the operations required by bus outbox notification.</summary>
/// <typeparam name="TScope">The scope type.</typeparam>
public interface IBusOutboxNotification<TScope>
    where TScope : class
{
    /// <summary>Waits for for delivery.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WaitForDeliveryAsync(CancellationToken cancellationToken);
    /// <summary>Delivers ed.</summary>
    void Delivered();
}
