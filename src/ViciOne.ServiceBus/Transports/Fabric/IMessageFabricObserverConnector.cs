namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message fabric observer connector.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IMessageFabricObserverConnector<out TContext>
    where TContext : class
{
    /// <summary>Connects message fabric observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectMessageFabricObserver(IMessageFabricObserver<TContext> observer);
}
