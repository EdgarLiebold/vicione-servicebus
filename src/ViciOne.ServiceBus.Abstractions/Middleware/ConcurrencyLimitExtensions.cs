using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Adjusts concurrency-limiting middleware through its control pipeline.</summary>
public static class ConcurrencyLimitExtensions
{
    /// <summary>Sets the maximum number of operations allowed to execute concurrently.</summary>
    /// <param name="pipe">The control pipeline connected to the concurrency limiter.</param>
    /// <param name="concurrencyLimit">The positive concurrency limit to apply.</param>
    /// <param name="timeProvider">The clock used to order this command against other adjustments.</param>
    /// <param name="cancellationToken">The token that cancels the adjustment.</param>
    /// <returns>A task that completes when every connected concurrency limiter has applied the command.</returns>
    public static Task SetConcurrencyLimitAsync(this IPipe<CommandContext> pipe, int concurrencyLimit, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        timeProvider ??= TimeProvider.System;
        var command = new SetConcurrencyLimitCommand(concurrencyLimit, timeProvider.GetUtcNow());

        return pipe.SendCommandAsync<SetConcurrencyLimit>(command, timeProvider, cancellationToken);
    }

    private sealed record SetConcurrencyLimitCommand(int ConcurrencyLimit, DateTimeOffset? Timestamp) : SetConcurrencyLimit
    {
        public string? LimiterId => null;
    }
}
