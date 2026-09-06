using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Describes requirements for in memory consume topology.</summary>
public interface IInMemoryConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
