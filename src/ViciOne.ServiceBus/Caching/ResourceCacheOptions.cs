using System;
using System.Threading;

#nullable enable
namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Immutable runtime policy used by <see cref="ResourceCache{TValue}"/>.
/// </summary>
public sealed class ResourceCacheOptions
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="capacity">The capacity value.</param>
    /// <param name="minAge">The min age value.</param>
    /// <param name="maxAge">The max age value.</param>
    /// <param name="expirationMode">The expiration mode value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="lifetimeCancellationToken">The lifetime cancellation token value.</param>
    /// <param name="cleanupInterval">The cleanup interval value.</param>
    public ResourceCacheOptions(int capacity = 1000, TimeSpan? minAge = null, TimeSpan? maxAge = null,
        ResourceCacheExpirationMode expirationMode = ResourceCacheExpirationMode.Sliding, TimeProvider? timeProvider = null,
        CancellationToken lifetimeCancellationToken = default, TimeSpan? cleanupInterval = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        var minimumAge = minAge ?? TimeSpan.Zero;
        var maximumAge = maxAge ?? TimeSpan.FromHours(24);

        if (minimumAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minAge), "Minimum age must not be negative.");
        if (maximumAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Maximum age must be greater than zero.");
        if (minimumAge > maximumAge)
            throw new ArgumentOutOfRangeException(nameof(minAge), "Minimum age must not exceed maximum age.");

        var interval = cleanupInterval ?? CalculateCleanupInterval(maximumAge);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cleanupInterval), "Cleanup interval must be greater than zero.");

        Capacity = capacity;
        MinAge = minimumAge;
        MaxAge = maximumAge;
        CleanupInterval = interval;
        ExpirationMode = expirationMode;
        TimeProvider = timeProvider ?? TimeProvider.System;
        LifetimeCancellationToken = lifetimeCancellationToken;
    }

    /// <summary>
    /// Gets the capacity value.
    /// </summary>
    public int Capacity { get; }
    /// <summary>
    /// Gets the min age value.
    /// </summary>
    public TimeSpan MinAge { get; }
    /// <summary>
    /// Gets the max age value.
    /// </summary>
    public TimeSpan MaxAge { get; }
    /// <summary>
    /// Gets the cleanup interval value.
    /// </summary>
    public TimeSpan CleanupInterval { get; }
    /// <summary>
    /// Gets the expiration mode value.
    /// </summary>
    public ResourceCacheExpirationMode ExpirationMode { get; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>
    /// Gets the lifetime cancellation token value.
    /// </summary>
    public CancellationToken LifetimeCancellationToken { get; }

    static TimeSpan CalculateCleanupInterval(TimeSpan maxAge)
    {
        var interval = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, maxAge.Ticks / 16));
        return interval > TimeSpan.FromMinutes(15) ? TimeSpan.FromMinutes(15) : interval;
    }
}
