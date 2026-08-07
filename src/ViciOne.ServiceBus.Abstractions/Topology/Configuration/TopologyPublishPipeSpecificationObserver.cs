// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class TopologyPublishPipeSpecificationObserver :
        IPublishPipeSpecificationObserver
    {
        readonly IPublishTopology _topology;

        public TopologyPublishPipeSpecificationObserver(IPublishTopology topology)
        {
            _topology = topology;
        }

        void IPublishPipeSpecificationObserver.MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        {
            IMessagePublishTopology<T> messagePublishTopology = _topology.GetMessageTopology<T>();

            var topologySpecification = new MessagePublishTopologyPipeSpecification<T>(messagePublishTopology);

            specification.AddParentMessageSpecification(topologySpecification);
        }
    }
}
