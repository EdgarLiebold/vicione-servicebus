namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Determines which timestamp is used when evaluating the maximum resource age.
/// </summary>
public enum ResourceCacheExpirationMode
{
    /// <summary>
    /// The maximum age is measured from the time the resource entered the cache.
    /// </summary>
    Absolute = 0,

    /// <summary>
    /// The maximum age is measured from the most recent cache or resource usage.
    /// </summary>
    Sliding = 1
}
