using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Identifies an Amazon SQS queue within a broker-topology builder.</summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>Gets the queue declaration represented by the handle.</summary>
    Queue Queue { get; }
}
