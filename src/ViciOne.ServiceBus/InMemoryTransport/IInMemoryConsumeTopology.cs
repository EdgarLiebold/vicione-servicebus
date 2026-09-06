using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by in memory consume topology.</summary>
public interface IInMemoryConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new IInMemoryMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Apply the entire topology to the builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
