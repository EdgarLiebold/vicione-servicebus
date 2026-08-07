// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IBusOutboxConfigurator
    {
        /// <summary>
        /// Disable the outbox message delivery service, removing the hosted service from the service collection
        /// </summary>
        void DisableDeliveryService();
    }
}
