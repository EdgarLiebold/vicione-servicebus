using System;
using System.Threading;

namespace ViciOne.ServiceBus.Caching;

/// <summary>Immutable runtime policy used by <see cref="ResourceCache{TValue}"/>.</summary>
public sealed class ResourceCacheOptions
{
    /// <summary>Initializes the immutable policy for a resource cache.</summary>
    /// <param name="capacity">The maximum number of committed and in-flight resources.</param>
    /// <param name="minAge">The minimum retention period before time-based expiration is allowed.</param>
    /// <param name="maxAge">The maximum absolute or sliding resource age.</param>
    /// <param name="expirationMode">The timestamp policy used to measure <paramref name="maxAge"/>.</param>
    /// <param name="timeProvider">The time source used for expiration and periodic cleanup.</param>
    /// <param name="lifetimeCancellationToken">The token that cancels cache-owned resource factories.</param>
    /// <param name="cleanupInterval">The interval between periodic expiration passes.</param>
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
        if (!Enum.IsDefined(expirationMode))
            throw new ArgumentOutOfRangeException(nameof(expirationMode), expirationMode, "Expiration mode is not defined.");

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

    /// <summary>Gets the maximum number of committed and in-flight resources.</summary>
    public int Capacity { get; }

    /// <summary>Gets the minimum retention period before time-based expiration is allowed.</summary>
    public TimeSpan MinAge { get; }

    /// <summary>Gets the maximum absolute or sliding resource age.</summary>
    public TimeSpan MaxAge { get; }

    /// <summary>Gets the interval between periodic expiration passes.</summary>
    public TimeSpan CleanupInterval { get; }

    /// <summary>Gets the timestamp policy used to measure maximum age.</summary>
    public ResourceCacheExpirationMode ExpirationMode { get; }

    /// <summary>Gets the time source used for expiration and periodic cleanup.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets the token that cancels cache-owned resource factories.</summary>
    public CancellationToken LifetimeCancellationToken { get; }

    static TimeSpan CalculateCleanupInterval(TimeSpan maxAge)
    {
        var interval = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, maxAge.Ticks / 16));
        return interval > TimeSpan.FromMinutes(15) ? TimeSpan.FromMinutes(15) : interval;
    }
}
