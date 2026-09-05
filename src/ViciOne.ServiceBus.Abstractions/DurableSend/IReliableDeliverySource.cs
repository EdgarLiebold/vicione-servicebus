using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Provider SPI for a persistence adapter whose due rows are executed by the bus's single reliable-messaging service.
/// Implementations do not own threads, timers or hosted-service registrations.
/// </summary>
public interface IReliableDeliverySource<TBus>
    where TBus : class, IBus
{
    /// <summary>Attempts one bounded unit of due work and reports whether progress was made.</summary>
    Task<bool> DeliverDueBatchAsync(CancellationToken cancellationToken = default);
}
