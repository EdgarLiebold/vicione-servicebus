namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Tracks the queue measurement.</summary>
public class QueueMetric
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    public QueueMetric(string name)
    {
        Name = name;
        DeliveryCount = new Counter();
        ActiveDeliveryCount = new Gauge();
        DelayedMessageCount = new Gauge();
        MessageCount = new Gauge();
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Total number of messages delivered.</summary>
    public Counter DeliveryCount { get; }

    /// <summary>Number of messages currently being delivered.</summary>
    public Gauge ActiveDeliveryCount { get; }

    /// <summary>Number of messages currently delayed before entering the queue.</summary>
    public Gauge DelayedMessageCount { get; }

    /// <summary>Number of messages currently in the queue (not including active messages).</summary>
    public Gauge MessageCount { get; }
}
