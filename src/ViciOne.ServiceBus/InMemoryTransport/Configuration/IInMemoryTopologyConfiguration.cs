using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Defines in memory topology configuration.</summary>
public interface IInMemoryTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets the publish.</summary>
    new IInMemoryPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets the consume.</summary>
    new IInMemoryConsumeTopologyConfigurator Consume { get; }
}
