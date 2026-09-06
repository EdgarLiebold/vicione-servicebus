using System;
using System.Threading;

namespace ViciOne.ServiceBus.Caching;

/// <summary>Immutable runtime policy used by <see cref="ResourceCache{TValue}"/>.</summary>
public sealed class ResourceCacheOptions
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="minAge">The min age.</param>
    /// <param name="maxAge">The max age.</param>
    /// <param name="expirationMode">The expiration mode.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="lifetimeCancellationToken">The lifetime cancellation token.</param>
    /// <param name="cleanupInterval">The cleanup interval.</param>
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

    /// <summary>Gets the capacity.</summary>
    public int Capacity { get; }
    /// <summary>Gets the min age.</summary>
    public TimeSpan MinAge { get; }
    /// <summary>Gets the max age.</summary>
    public TimeSpan MaxAge { get; }
    /// <summary>Gets the cleanup interval.</summary>
    public TimeSpan CleanupInterval { get; }
    /// <summary>Gets the expiration mode.</summary>
    public ResourceCacheExpirationMode ExpirationMode { get; }
    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>Gets the lifetime cancellation token.</summary>
    public CancellationToken LifetimeCancellationToken { get; }

    static TimeSpan CalculateCleanupInterval(TimeSpan maxAge)
    {
        var interval = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, maxAge.Ticks / 16));
        return interval > TimeSpan.FromMinutes(15) ? TimeSpan.FromMinutes(15) : interval;
    }
}
