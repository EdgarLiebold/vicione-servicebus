namespace ViciOne.ServiceBus;

using System;
using System.Threading;
using Caching;


/// <summary>
/// Immutable cache settings for queue and topic provider contexts owned by one Amazon SQS connection.
/// </summary>
public sealed class AmazonSqsClientContextCacheOptions
{
    public AmazonSqsClientContextCacheOptions()
        : this(1000, TimeSpan.FromDays(427), TimeProvider.System)
    {
    }

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

    public int Capacity { get; }
    public TimeSpan MaxAge { get; }
    public TimeProvider TimeProvider { get; }

    internal ResourceCacheOptions CreateResourceCacheOptions(CancellationToken lifetimeCancellationToken)
    {
        return new ResourceCacheOptions(Capacity, TimeSpan.Zero, MaxAge, ResourceCacheExpirationMode.Sliding, TimeProvider,
            lifetimeCancellationToken);
    }
}
