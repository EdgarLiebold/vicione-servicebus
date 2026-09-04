using System;

namespace ViciOne.ServiceBus.Configuration;

public class TopologyPublishPipeSpecificationObserver :
    IPublishPipeSpecificationObserver
{
    readonly IPublishTopology _topology;

    public TopologyPublishPipeSpecificationObserver(IPublishTopology topology)
    {
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
    }

    void IPublishPipeSpecificationObserver.MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessagePublishTopology<T> messagePublishTopology = _topology.GetMessageTopology<T>();

        var topologySpecification = new MessagePublishTopologyPipeSpecification<T>(messagePublishTopology);

        specification.AddParentMessageSpecification(topologySpecification);
    }
}
