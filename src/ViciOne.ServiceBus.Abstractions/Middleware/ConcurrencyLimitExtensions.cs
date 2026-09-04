using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus;

public static class ConcurrencyLimitExtensions
{
    /// <summary>
    /// Set the concurrency limit of the filter
    /// </summary>
    /// <param name="pipe"></param>
    /// <param name="concurrencyLimit"></param>
    /// <param name="timeProvider">The clock used to timestamp the concurrency-limit command and enforce its timeout.</param>
    /// <returns></returns>
    public static Task SetConcurrencyLimit(this IPipe<CommandContext> pipe, int concurrencyLimit, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;

        return pipe.SendCommand<SetConcurrencyLimit>(new Limit(concurrencyLimit, timeProvider.GetUtcNow().UtcDateTime), timeProvider);
    }


    class Limit :
        SetConcurrencyLimit
    {
        public Limit(int concurrencyLimit, DateTime timestamp)
        {
            ConcurrencyLimit = concurrencyLimit;
            Timestamp = timestamp;
        }

        public DateTime? Timestamp { get; }
        public string? Id => null;
        public int ConcurrencyLimit { get; }
    }
}
