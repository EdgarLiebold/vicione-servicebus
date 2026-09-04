namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message fabric observer connector.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IMessageFabricObserverConnector<out TContext>
    where TContext : class
{
    /// <summary>
    /// Connects message fabric observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectMessageFabricObserver(IMessageFabricObserver<TContext> observer);
}
