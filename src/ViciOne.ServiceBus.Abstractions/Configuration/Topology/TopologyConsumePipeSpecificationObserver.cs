using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds consume topology to each newly created message consume-pipe specification.</summary>
public sealed class TopologyConsumePipeSpecificationObserver :
    IConsumePipeSpecificationObserver
{
    readonly IConsumeTopology _topology;

    /// <summary>Initializes the observer with the consume topology to apply.</summary>
    /// <param name="topology">The consume topology.</param>
    public TopologyConsumePipeSpecificationObserver(IConsumeTopology topology)
    {
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
    }

    void IConsumePipeSpecificationObserver.MessageSpecificationCreated<T>(IMessageConsumePipeSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessageConsumeTopology<T> messageConsumeTopology = _topology.GetMessageTopology<T>();

        var topologySpecification = new MessageConsumeTopologyPipeSpecification<T>(messageConsumeTopology);

        specification.AddParentMessageSpecification(topologySpecification);
    }
}
