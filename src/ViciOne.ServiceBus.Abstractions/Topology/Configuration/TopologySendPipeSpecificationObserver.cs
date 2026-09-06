using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes topology send pipe specification events.</summary>
public class TopologySendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendTopology _topology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topology">The topology.</param>
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
