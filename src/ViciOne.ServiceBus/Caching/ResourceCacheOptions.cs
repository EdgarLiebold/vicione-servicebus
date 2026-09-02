#nullable enable
namespace ViciOne.ServiceBus.Caching
{
    using System;
    using System.Threading;


    /// <summary>
    /// Immutable runtime policy used by <see cref="ResourceCache{TValue}"/>.
    /// </summary>
    public sealed class ResourceCacheOptions
    {
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

        public int Capacity { get; }
        public TimeSpan MinAge { get; }
        public TimeSpan MaxAge { get; }
        public TimeSpan CleanupInterval { get; }
        public ResourceCacheExpirationMode ExpirationMode { get; }
        public TimeProvider TimeProvider { get; }
        public CancellationToken LifetimeCancellationToken { get; }

        static TimeSpan CalculateCleanupInterval(TimeSpan maxAge)
        {
            var interval = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, maxAge.Ticks / 16));
            return interval > TimeSpan.FromMinutes(15) ? TimeSpan.FromMinutes(15) : interval;
        }
    }
}
