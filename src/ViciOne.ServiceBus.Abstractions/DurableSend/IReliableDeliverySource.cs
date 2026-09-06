using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Provider SPI for a persistence adapter whose due rows are executed by the bus's single reliable-messaging service.
/// Implementations do not own threads, timers or hosted-service registrations.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IReliableDeliverySource<TBus>
    where TBus : class, IBus
{
    /// <summary>Attempts one bounded unit of due work and reports whether progress was made.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the deliver due batch outcome.</returns>
    Task<bool> DeliverDueBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>Waits until work may be available or the source's bounded polling interval elapses.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WaitForWorkAsync(CancellationToken cancellationToken = default);
}
