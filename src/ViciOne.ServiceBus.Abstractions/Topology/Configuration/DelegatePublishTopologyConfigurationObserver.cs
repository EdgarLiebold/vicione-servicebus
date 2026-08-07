// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class DelegatePublishTopologyConfigurationObserver :
        IPublishTopologyConfigurationObserver
    {
        readonly IPublishTopologyConfigurator _publishTopology;

        public DelegatePublishTopologyConfigurationObserver(IPublishTopologyConfigurator publishTopology)
        {
            _publishTopology = publishTopology;
        }

        public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
            where T : class
        {
            IMessagePublishTopologyConfigurator<T> publishTopologyConfigurator = _publishTopology.GetMessageTopology<T>();

            configurator.AddDelegate(publishTopologyConfigurator);
        }
    }
}
