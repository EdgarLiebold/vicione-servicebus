using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds send topology to each newly created message send-pipe specification.</summary>
public sealed class TopologySendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendTopology _topology;

    /// <summary>Initializes the observer with the send topology to apply.</summary>
    /// <param name="topology">The send topology.</param>
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
