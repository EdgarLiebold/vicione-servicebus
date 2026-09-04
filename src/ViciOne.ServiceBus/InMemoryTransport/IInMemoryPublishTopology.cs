namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory publish topology.
/// </summary>
public interface IInMemoryPublishTopology :
    IPublishTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IInMemoryMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;
}
