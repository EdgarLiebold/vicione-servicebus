using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Provides extension methods for concurrency limit.</summary>
public static class ConcurrencyLimitExtensions
{
    /// <summary>Set the concurrency limit of the filter.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="concurrencyLimit">The concurrency limit.</param>
    /// <param name="timeProvider">The clock used to timestamp the concurrency-limit command and enforce its timeout.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SetConcurrencyLimitAsync(this IPipe<CommandContext> pipe, int concurrencyLimit, TimeProvider? timeProvider = null, CancellationToken cancellationToken = default)
    {
        timeProvider ??= TimeProvider.System;

        return pipe.SendCommandAsync<SetConcurrencyLimit>(new Limit(concurrencyLimit, timeProvider.GetUtcNow()), timeProvider, cancellationToken: cancellationToken);
    }


    class Limit :
        SetConcurrencyLimit
    {
        public Limit(int concurrencyLimit, DateTimeOffset timestamp)
        {
            ConcurrencyLimit = concurrencyLimit;
            Timestamp = timestamp;
        }

        public DateTimeOffset? Timestamp { get; }
        public string? Id => null;
        public int ConcurrencyLimit { get; }
    }
}
