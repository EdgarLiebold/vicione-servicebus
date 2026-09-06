using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes topology publish pipe specification events.</summary>
public class TopologyPublishPipeSpecificationObserver :
    IPublishPipeSpecificationObserver
{
    readonly IPublishTopology _topology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topology">The topology.</param>
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
