namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached access to implemented message type data.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IImplementedMessageTypeCache<TMessage>
    where TMessage : class
{
    /// <summary>Invokes the interface for each implemented type of the message.</summary>
    /// <param name="implementedMessageType">The runtime implemented message type used by the operation.</param>
    void EnumerateImplementedTypes(IImplementedMessageType implementedMessageType);
}
