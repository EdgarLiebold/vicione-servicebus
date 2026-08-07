// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class TopologyConsumePipeSpecificationObserver :
        IConsumePipeSpecificationObserver
    {
        readonly IConsumeTopology _topology;

        public TopologyConsumePipeSpecificationObserver(IConsumeTopology topology)
        {
            _topology = topology;
        }

        void IConsumePipeSpecificationObserver.MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
        {
            IMessageConsumeTopology<T> messagePublishTopology = _topology.GetMessageTopology<T>();

            var topologySpecification = new MessageConsumeTopologyPipeSpecification<T>(messagePublishTopology);

            specification.AddParentMessageSpecification(topologySpecification);
        }
    }
}
