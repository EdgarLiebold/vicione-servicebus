using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Defines the contract for in memory delay provider.
/// </summary>
public interface IInMemoryDelayProvider
{
    /// <summary>
    /// Gets the utc now value.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Performs the delay operation.
    /// </summary>
    /// <param name="delay">The delay value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the delay operation.
    /// </summary>
    /// <param name="delayUntil">The delay until value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the advance operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    void Advance(TimeSpan duration);
}
