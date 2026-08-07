// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class TopologySendPipeSpecificationObserver :
        ISendPipeSpecificationObserver
    {
        readonly ISendTopology _topology;

        public TopologySendPipeSpecificationObserver(ISendTopology topology)
        {
            _topology = topology;
        }

        void ISendPipeSpecificationObserver.MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        {
            IMessageSendTopology<T> messageSendTopology = _topology.GetMessageTopology<T>();

            var topologySpecification = new MessageSendTopologyPipeSpecification<T>(messageSendTopology);

            specification.AddParentMessageSpecification(topologySpecification);
        }
    }
}
