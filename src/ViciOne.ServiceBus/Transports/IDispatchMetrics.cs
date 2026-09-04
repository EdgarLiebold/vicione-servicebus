namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for dispatch metrics.
/// </summary>
public interface IDispatchMetrics
{
    /// <summary>
    /// Gets the active dispatch count value.
    /// </summary>
    int ActiveDispatchCount { get; }
    /// <summary>
    /// Gets the dispatch count value.
    /// </summary>
    long DispatchCount { get; }
    /// <summary>
    /// Gets the max concurrent dispatch count value.
    /// </summary>
    int MaxConcurrentDispatchCount { get; }

    /// <summary>
    /// Occurs when zero activity.
    /// </summary>
    event ZeroActiveDispatchHandler ZeroActivity;

    /// <summary>
    /// Gets metrics.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    DeliveryMetrics GetMetrics();
}
