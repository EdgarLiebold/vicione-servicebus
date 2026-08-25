namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class TopologySendPipeSpecificationObserver :
        ISendPipeSpecificationObserver
    {
        readonly ISendTopology _topology;

        public TopologySendPipeSpecificationObserver(ISendTopology topology)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        void ISendPipeSpecificationObserver.MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        {
            ArgumentNullException.ThrowIfNull(specification);

            IMessageSendTopology<T> messageSendTopology = _topology.GetMessageTopology<T>();

            var topologySpecification = new MessageSendTopologyPipeSpecification<T>(messageSendTopology);

            specification.AddParentMessageSpecification(topologySpecification);
        }
    }
}
