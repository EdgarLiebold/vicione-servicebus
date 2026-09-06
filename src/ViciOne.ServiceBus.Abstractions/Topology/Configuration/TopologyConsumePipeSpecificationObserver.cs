namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes topology consume pipe specification events.</summary>
public class TopologyConsumePipeSpecificationObserver :
    IConsumePipeSpecificationObserver
{
    readonly IConsumeTopology _topology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topology">The topology.</param>
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
