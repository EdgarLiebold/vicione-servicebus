namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga connector factory.
/// </summary>
public interface ISagaConnectorFactory
{
    /// <summary>
    /// Creates message connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    ISagaMessageConnector<T> CreateMessageConnector<T>()
        where T : class, ISaga;
}
