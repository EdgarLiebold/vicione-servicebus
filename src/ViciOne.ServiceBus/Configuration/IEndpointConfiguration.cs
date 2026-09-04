namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint configuration.
/// </summary>
public interface IEndpointConfiguration :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IReceivePipelineConfigurator,
    ISpecification
{
    /// <summary>
    /// Gets the is bus endpoint value.
    /// </summary>
    bool IsBusEndpoint { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    IConsumePipeConfiguration Consume { get; }
    /// <summary>
    /// Gets the send value.
    /// </summary>
    ISendPipeConfiguration Send { get; }
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    IPublishPipeConfiguration Publish { get; }
    /// <summary>
    /// Gets the receive value.
    /// </summary>
    IReceivePipeConfiguration Receive { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    ITopologyConfiguration Topology { get; }

    /// <summary>
    /// Gets the serialization value.
    /// </summary>
    ISerializationConfiguration Serialization { get; }

    /// <summary>
    /// Gets the transport value.
    /// </summary>
    ITransportConfiguration Transport { get; }
}
