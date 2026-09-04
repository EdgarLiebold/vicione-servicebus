using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory consume topology specification.
/// </summary>
public interface IInMemoryConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
