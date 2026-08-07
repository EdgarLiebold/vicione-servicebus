// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IActiveMqQueueBindingConfigurator :
        IActiveMqQueueConfigurator
    {
        /// <summary>
        /// A routing key for the exchange binding
        /// </summary>
        string Selector { set; }
    }
}
