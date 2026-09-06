namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message queue.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageQueue<in TContext, T> :
    IMessageSink<T>
    where TContext : class
    where T : class
{
    /// <summary>Gets the name.</summary>
    string Name { get; }

    /// <summary>Connects message receiver.</summary>
    /// <param name="nodeContext">The node context.</param>
    /// <param name="receiver">The receiver.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    TopologyHandle ConnectMessageReceiver(TContext nodeContext, IMessageReceiver<T> receiver);
}
