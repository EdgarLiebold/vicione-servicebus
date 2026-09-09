namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached access to the polymorphic topology parents of a message contract.</summary>
/// <typeparam name="TMessage">The message contract whose topology is inspected.</typeparam>
public interface IImplementedMessageTypeCache<TMessage>
    where TMessage : class
{
    /// <summary>Invokes a consumer for each implemented message contract.</summary>
    /// <param name="implementedMessageType">The consumer invoked for each topology parent.</param>
    void EnumerateImplementedTypes(IImplementedMessageType implementedMessageType);
}
