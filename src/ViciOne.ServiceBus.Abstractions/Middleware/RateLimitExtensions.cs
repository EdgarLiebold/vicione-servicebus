using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus;

public static class RateLimitExtensions
{
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
