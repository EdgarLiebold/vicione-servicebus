using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Defines the contract for bus outbox notification.
/// </summary>
/// <typeparam name="TScope">The t scope type.</typeparam>
public interface IBusOutboxNotification<TScope>
    where TScope : class
{
    /// <summary>
    /// Performs the wait for delivery operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task WaitForDeliveryAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Performs the delivered operation.
    /// </summary>
    void Delivered();
}
