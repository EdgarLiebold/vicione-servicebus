using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Represents the lifecycle and delivery metrics of an Event Hubs data receiver.</summary>
public interface IEventHubDataReceiver :
    IAgent,
    IDeliveryMetrics
{
}
