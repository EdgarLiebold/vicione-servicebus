using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Identifies a queue within its owning SQL topology builder.</summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>Gets the queue.</summary>
    Queue Queue { get; }
}
