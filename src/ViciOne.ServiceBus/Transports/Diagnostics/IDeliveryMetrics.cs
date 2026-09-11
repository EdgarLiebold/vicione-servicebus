namespace ViciOne.ServiceBus.Transports;

/// <summary>Reports cumulative and peak message-delivery measurements.</summary>
public interface IDeliveryMetrics
{
    /// <summary>Gets the cumulative number of messages delivered by the component.</summary>
    long DeliveryCount { get; }

    /// <summary>Gets the highest number of messages dispatched concurrently.</summary>
    int MaxConcurrentDeliveryCount { get; }
}
