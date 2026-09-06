using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Provides in memory delay services.</summary>
public interface IInMemoryDelayProvider
{
    /// <summary>Gets the utc now.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Delays the operation for the configured duration.</summary>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    /// <summary>Delays the operation for the configured duration.</summary>
    /// <param name="delayUntil">The delay until.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    /// <summary>Advances the current state.</summary>
    /// <param name="duration">The duration.</param>
    void Advance(TimeSpan duration);
}
