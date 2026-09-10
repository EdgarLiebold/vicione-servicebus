using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides the logical clock and delay scheduling used by the in-memory transport.</summary>
public interface IInMemoryDelayProvider
{
    /// <summary>Gets the current logical UTC time.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Returns a task that completes after a relative logical-time delay.</summary>
    /// <param name="delay">The non-negative delay duration.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    /// <summary>Returns a task that completes at an absolute logical-time deadline.</summary>
    /// <param name="delayUntil">The UTC deadline.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default);

    /// <summary>Advances logical time and releases every delay whose deadline is reached.</summary>
    /// <param name="duration">The positive duration by which logical time advances.</param>
    void Advance(TimeSpan duration);
}
