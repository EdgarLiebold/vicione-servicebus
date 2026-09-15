using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds publish topology to each newly created message publish-pipe specification.</summary>
public sealed class TopologyPublishPipeSpecificationObserver :
    IPublishPipeSpecificationObserver
{
    readonly IPublishTopology _topology;

    /// <summary>Initializes the observer with the publish topology to apply.</summary>
    /// <param name="topology">The publish topology.</param>
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
