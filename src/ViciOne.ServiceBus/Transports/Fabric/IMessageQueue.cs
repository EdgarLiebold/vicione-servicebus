namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message queue.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageQueue<in TContext, T> :
    IMessageSink<T>
    where TContext : class
    where T : class
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Connects message receiver.
    /// </summary>
    /// <param name="nodeContext">The node context value.</param>
    /// <param name="receiver">The receiver value.</param>
    /// <returns>The result of the operation.</returns>
    TopologyHandle ConnectMessageReceiver(TContext nodeContext, IMessageReceiver<T> receiver);
}
