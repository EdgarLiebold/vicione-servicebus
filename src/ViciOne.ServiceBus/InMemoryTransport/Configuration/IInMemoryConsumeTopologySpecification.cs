using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Validates and applies one in-memory consume-topology operation.</summary>
internal interface IInMemoryConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies the operation to a consume topology builder.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
