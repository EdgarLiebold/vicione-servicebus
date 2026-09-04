using System;
using System.Threading;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>
/// Immutable cache settings for queue and topic provider contexts owned by one Amazon SQS connection.
/// </summary>
public sealed class AmazonSqsClientContextCacheOptions
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AmazonSqsClientContextCacheOptions()
        : this(1000, TimeSpan.FromDays(427), TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="capacity">The capacity value.</param>
    /// <param name="maxAge">The max age value.</param>
    /// <param name="timeProvider">The time provider value.</param>
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

    /// <summary>
    /// Gets the capacity value.
    /// </summary>
    public int Capacity { get; }
    /// <summary>
    /// Gets the max age value.
    /// </summary>
    public TimeSpan MaxAge { get; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    internal ResourceCacheOptions CreateResourceCacheOptions(CancellationToken lifetimeCancellationToken)
    {
        return new ResourceCacheOptions(Capacity, TimeSpan.Zero, MaxAge, ResourceCacheExpirationMode.Sliding, TimeProvider,
            lifetimeCancellationToken);
    }
}
