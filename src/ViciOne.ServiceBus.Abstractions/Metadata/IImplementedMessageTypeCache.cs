namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for implemented message type cache.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IImplementedMessageTypeCache<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Invokes the interface for each implemented type of the message
    /// </summary>
    /// <param name="implementedMessageType"></param>
    void EnumerateImplementedTypes(IImplementedMessageType implementedMessageType);
}
