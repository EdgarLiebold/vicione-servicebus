namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures bus outbox.</summary>
public interface IBusOutboxConfigurator
{
    /// <summary>Disable the outbox message delivery service, removing the hosted service from the service collection.</summary>
    void DisableDeliveryService();
}
