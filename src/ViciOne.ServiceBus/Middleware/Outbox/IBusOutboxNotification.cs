using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Coordinates persisted outbox work with the single delivery agent for a bus scope.</summary>
/// <typeparam name="TScope">The bus and persistence scope whose delivery agent is notified.</typeparam>
public interface IBusOutboxNotification<TScope>
    where TScope : class
{
    /// <summary>Waits until persisted work is signaled or the configured polling interval elapses.</summary>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes when the delivery agent should query for work.</returns>
    Task WaitForDeliveryAsync(CancellationToken cancellationToken);

    /// <summary>Signals that persisted outbox work is available for delivery.</summary>
    void SignalDelivery();
}
