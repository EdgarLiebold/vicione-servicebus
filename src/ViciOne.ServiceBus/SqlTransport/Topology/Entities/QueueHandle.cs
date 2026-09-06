using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Controls the lifetime of queue.</summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>Gets the queue.</summary>
    Queue Queue { get; }
}
