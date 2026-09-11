namespace ViciOne.ServiceBus.Transports;

/// <summary>Reports message-dispatch activity and notifies observers when all dispatches complete.</summary>
public interface IDispatchMetrics
{
    /// <summary>Gets the number of dispatches currently in progress.</summary>
    int ActiveDispatchCount { get; }
    /// <summary>Gets the cumulative number of dispatches.</summary>
    long DispatchCount { get; }
    /// <summary>Gets the highest number of dispatches observed concurrently.</summary>
    int MaxConcurrentDispatchCount { get; }

    /// <summary>Occurs after the active dispatch count reaches zero.</summary>
    event ZeroActivityHandler ZeroActivity;

    /// <summary>Captures the current cumulative and peak delivery measurements.</summary>
    /// <returns>An immutable delivery-metrics snapshot.</returns>
    IDeliveryMetrics GetMetrics();
}
