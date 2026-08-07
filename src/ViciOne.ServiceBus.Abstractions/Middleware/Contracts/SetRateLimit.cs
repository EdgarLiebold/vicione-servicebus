// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts
{
    /// <summary>
    /// Set the rate limit of the RateLimitFilter
    /// </summary>
    public interface SetRateLimit
    {
        /// <summary>
        /// The new rate limit for the filter
        /// </summary>
        int RateLimit { get; }
    }
}
