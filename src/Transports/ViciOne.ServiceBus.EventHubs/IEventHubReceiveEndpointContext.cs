using ViciOne.ServiceBus.EventHubs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Provides receive-pipeline state and access to the Event Hubs processor supervisor.</summary>
public interface IEventHubReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the supervisor that owns the Event Hubs processor context.</summary>
    IProcessorContextSupervisor ContextSupervisor { get; }
}
