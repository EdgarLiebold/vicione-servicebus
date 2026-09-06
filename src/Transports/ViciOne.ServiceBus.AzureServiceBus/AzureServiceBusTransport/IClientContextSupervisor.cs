using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises the lifecycle of an Azure Service Bus processor client context.</summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
