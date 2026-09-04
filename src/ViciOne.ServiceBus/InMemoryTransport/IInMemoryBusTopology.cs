namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory bus topology.
/// </summary>
public interface IInMemoryBusTopology :
    IBusTopology
{
    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IInMemoryMessagePublishTopology<T> Publish<T>()
        where T : class;
}
