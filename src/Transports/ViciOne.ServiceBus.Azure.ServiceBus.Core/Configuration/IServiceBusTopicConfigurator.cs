// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IServiceBusTopicConfigurator :
        IServiceBusMessageEntityConfigurator,
        ISpecification
    {
        /// <summary>
        /// If True, the topic will deliver messages to subscriptions in order
        /// </summary>
        bool? SupportOrdering { set; }
    }
}
