namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by dispatch metrics.</summary>
public interface IDispatchMetrics
{
    /// <summary>Gets the active dispatch count.</summary>
    int ActiveDispatchCount { get; }
    /// <summary>Gets the dispatch count.</summary>
    long DispatchCount { get; }
    /// <summary>Gets the max concurrent dispatch count.</summary>
    int MaxConcurrentDispatchCount { get; }

    /// <summary>Occurs when zero activity.</summary>
    event ZeroActiveDispatchHandler ZeroActivity;

    /// <summary>Gets metrics.</summary>
    /// <returns>The metrics.</returns>
    DeliveryMetrics GetMetrics();
}
