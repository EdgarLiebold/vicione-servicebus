namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls background delivery for a durable bus outbox.</summary>
public interface IBusOutboxConfigurator
{
    /// <summary>Disables the hosted service that delivers stored outbox messages to the transport.</summary>
    void DisableDeliveryService();
}
