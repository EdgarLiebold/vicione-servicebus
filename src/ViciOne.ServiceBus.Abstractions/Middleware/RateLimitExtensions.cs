using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Adjusts rate-limiting middleware through its control pipeline.</summary>
public static class RateLimitExtensions
{
    /// <summary>Sets the maximum number of operations admitted during each configured interval.</summary>
    /// <param name="pipe">The control pipeline connected to the rate limiter.</param>
    /// <param name="rateLimit">The positive number of operations admitted per interval.</param>
    /// <param name="cancellationToken">The token that cancels the adjustment.</param>
    /// <returns>A task that completes when every connected rate limiter has applied the command.</returns>
    public static Task SetRateLimitAsync(this IPipe<CommandContext> pipe, int rateLimit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);

        return pipe.SendCommandAsync<SetRateLimit>(new SetRateLimitCommand(rateLimit), cancellationToken: cancellationToken);
    }

    private sealed record SetRateLimitCommand(int RateLimit) : SetRateLimit;
}
