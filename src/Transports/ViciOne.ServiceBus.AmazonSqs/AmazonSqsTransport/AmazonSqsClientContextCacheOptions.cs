using System;
using System.Threading;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Immutable cache settings for queue and topic provider contexts owned by one Amazon SQS connection.</summary>
public sealed class AmazonSqsClientContextCacheOptions
{
    /// <summary>Creates cache settings with a capacity of 1,000 entries and sliding expiration of 427 days.</summary>
    public AmazonSqsClientContextCacheOptions()
        : this(1000, TimeSpan.FromDays(427), TimeProvider.System)
    {
    }

    /// <summary>Creates validated sliding-expiration settings for AWS entity caches.</summary>
    /// <param name="capacity">The maximum number of cached entity contexts.</param>
    /// <param name="maxAge">The sliding expiration interval.</param>
    /// <param name="timeProvider">The clock used to evaluate expiration.</param>
    public AmazonSqsClientContextCacheOptions(int capacity, TimeSpan maxAge, TimeProvider timeProvider)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        if (maxAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Maximum age must be greater than zero.");

        Capacity = capacity;
        MaxAge = maxAge;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the maximum number of cached entity contexts.</summary>
    public int Capacity { get; }
    /// <summary>Gets the sliding expiration interval.</summary>
    public TimeSpan MaxAge { get; }
    /// <summary>Gets the clock used to evaluate expiration.</summary>
    public TimeProvider TimeProvider { get; }

    internal ResourceCacheOptions CreateResourceCacheOptions(CancellationToken lifetimeCancellationToken)
    {
        return new ResourceCacheOptions(Capacity, TimeSpan.Zero, MaxAge, ResourceCacheExpirationMode.Sliding, TimeProvider,
            lifetimeCancellationToken);
    }
}
