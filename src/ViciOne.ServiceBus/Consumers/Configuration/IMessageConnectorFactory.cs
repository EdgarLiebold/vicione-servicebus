namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message connector factory.
/// </summary>
public interface IMessageConnectorFactory
{
    /// <summary>
    /// Creates consumer connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IConsumerMessageConnector<T> CreateConsumerConnector<T>()
        where T : class;

    /// <summary>
    /// Creates instance connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IInstanceMessageConnector<T> CreateInstanceConnector<T>()
        where T : class;
}
