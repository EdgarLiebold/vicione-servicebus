using ViciOne.ServiceBus.EventHubs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub receive endpoint context.
/// </summary>
public interface IEventHubReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the context supervisor value.
    /// </summary>
    IProcessorContextSupervisor ContextSupervisor { get; }
}
