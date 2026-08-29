namespace ViciOne.ServiceBus;

using System;
using Internals.Caching;


/// <summary>
/// Immutable cache settings for queue and topic provider contexts owned by one Amazon SQS host.
/// </summary>
public sealed class AmazonSqsClientContextCacheOptions
{
    public AmazonSqsClientContextCacheOptions()
        : this(1000, TimeSpan.FromDays(427), TimeProvider.System)
    {
    }

    public AmazonSqsClientContextCacheOptions(int capacity, TimeSpan maxAge, TimeProvider timeProvider)
    {
        if (capacity < 8)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 8.");
        if (maxAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Maximum age must be greater than zero.");

        Capacity = capacity;
        MaxAge = maxAge;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public int Capacity { get; }
    public TimeSpan MaxAge { get; }
    public TimeProvider TimeProvider { get; }

    internal ICache<TKey, TValue, ITimeToLiveCacheValue<TValue>> CreateCache<TKey, TValue>()
        where TValue : class
    {
        var options = new CacheOptions { Capacity = Capacity };
        var policy = new TimeToLiveCachePolicy<TValue>(MaxAge, TimeProvider);

        return new ViciOneServiceBusCache<TKey, TValue, ITimeToLiveCacheValue<TValue>>(policy, options);
    }
}
