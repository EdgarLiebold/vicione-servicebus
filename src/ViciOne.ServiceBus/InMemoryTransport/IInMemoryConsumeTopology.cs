using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory consume topology.
/// </summary>
public interface IInMemoryConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IInMemoryMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
