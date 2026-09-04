using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a topology send pipe specification observer implementation.
/// </summary>
public class TopologySendPipeSpecificationObserver :
    ISendPipeSpecificationObserver
{
    readonly ISendTopology _topology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topology">The topology value.</param>
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
