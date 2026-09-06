using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises the lifecycle of an Azure Service Bus send-endpoint context.</summary>
public interface ISendEndpointContextSupervisor :
    ITransportSupervisor<SendEndpointContext>
{
}
