namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a topology consume pipe specification observer implementation.
/// </summary>
public class TopologyConsumePipeSpecificationObserver :
    IConsumePipeSpecificationObserver
{
    readonly IConsumeTopology _topology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topology">The topology value.</param>
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
