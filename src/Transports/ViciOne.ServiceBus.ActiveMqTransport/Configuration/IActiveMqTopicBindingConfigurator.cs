// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Used to configure the binding of an exchange (to either a queue or another exchange)
    /// </summary>
    public interface IActiveMqTopicBindingConfigurator :
        IActiveMqTopicConfigurator
    {
        /// <summary>
        /// A routing key for the exchange binding
        /// </summary>
        string Selector { set; }
    }
}
