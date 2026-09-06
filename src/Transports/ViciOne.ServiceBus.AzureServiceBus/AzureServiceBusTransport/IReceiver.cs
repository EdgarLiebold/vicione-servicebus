using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Controls an Azure Service Bus processor and exposes its delivery metrics.</summary>
public interface IReceiver :
    IAgent,
    DeliveryMetrics
{
    /// <summary>Starts message processing on the underlying Azure processor.</summary>
    void Start();
}
