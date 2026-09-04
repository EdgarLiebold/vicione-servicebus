namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Specifies the available bus health status values.
/// </summary>
public enum BusHealthStatus
{
    /// <summary>
    /// Indicates unhealthy.
    /// </summary>
    Unhealthy = 0,
    /// <summary>
    /// Indicates degraded.
    /// </summary>
    Degraded = 1,
    /// <summary>
    /// Indicates healthy.
    /// </summary>
    Healthy = 2
}
