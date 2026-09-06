using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Provides extension methods for rate limit.</summary>
public static class RateLimitExtensions
{
    /// <summary>Sets rate limit.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="rateLimit">The rate limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SetRateLimitAsync(this IPipe<CommandContext> pipe, int rateLimit, CancellationToken cancellationToken = default)
    {
        return pipe.SendCommandAsync<SetRateLimit>(new Limit(rateLimit), cancellationToken: cancellationToken);
    }


    class Limit :
        SetRateLimit
    {
        public Limit(int rateLimit)
        {
            RateLimit = rateLimit;
        }

        public int RateLimit { get; }
    }
}
