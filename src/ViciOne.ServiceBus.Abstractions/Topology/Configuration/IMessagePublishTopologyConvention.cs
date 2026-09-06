namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by message publish topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePublishTopologyConvention<TMessage> :
    IMessagePublishTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to get message publish topology.</summary>
    /// <param name="messagePublishTopology">Receives the message publish topology produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopology(out IMessagePublishTopology<TMessage> messagePublishTopology);
}


/// <summary>Defines the operations required by message publish topology convention.</summary>
public interface IMessagePublishTopologyConvention
{
    /// <summary>Attempts to get message publish topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopologyConvention<T>(out IMessagePublishTopologyConvention<T> convention)
        where T : class;
}
