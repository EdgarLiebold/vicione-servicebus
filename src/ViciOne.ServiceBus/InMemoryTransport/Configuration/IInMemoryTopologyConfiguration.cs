using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Combines transport-independent topology with in-memory publish and consume topology.</summary>
internal interface IInMemoryTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets mutable in-memory publish topology.</summary>
    new IInMemoryPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets mutable in-memory consume topology.</summary>
    new IInMemoryConsumeTopologyConfigurator Consume { get; }
}
