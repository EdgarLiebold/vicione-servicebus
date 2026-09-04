using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory topology configuration.
/// </summary>
public interface IInMemoryTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new IInMemoryPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new IInMemoryConsumeTopologyConfigurator Consume { get; }
}
