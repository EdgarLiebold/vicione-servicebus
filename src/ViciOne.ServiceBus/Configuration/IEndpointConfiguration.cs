namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines endpoint configuration.</summary>
public interface IEndpointConfiguration :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IReceivePipelineConfigurator,
    ISpecification
{
    /// <summary>Gets a value indicating whether bus endpoint.</summary>
    bool IsBusEndpoint { get; }

    /// <summary>Gets the consume.</summary>
    IConsumePipeConfiguration Consume { get; }
    /// <summary>Gets the send.</summary>
    ISendPipeConfiguration Send { get; }
    /// <summary>Gets the publish.</summary>
    IPublishPipeConfiguration Publish { get; }
    /// <summary>Gets the receive.</summary>
    IReceivePipeConfiguration Receive { get; }

    /// <summary>Gets the topology.</summary>
    ITopologyConfiguration Topology { get; }

    /// <summary>Gets the serialization.</summary>
    ISerializationConfiguration Serialization { get; }

    /// <summary>Gets the transport.</summary>
    ITransportConfiguration Transport { get; }
}
