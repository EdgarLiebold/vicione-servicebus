namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message publish topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessagePublishTopologyConvention<TMessage> :
    IMessagePublishTopologyConvention
    where TMessage : class
{
    /// <summary>
    /// Attempts to get message publish topology.
    /// </summary>
    /// <param name="messagePublishTopology">The message publish topology value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopology(out IMessagePublishTopology<TMessage> messagePublishTopology);
}


/// <summary>
/// Defines the contract for message publish topology convention.
/// </summary>
public interface IMessagePublishTopologyConvention
{
    /// <summary>
    /// Attempts to get message publish topology convention.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopologyConvention<T>(out IMessagePublishTopologyConvention<T> convention)
        where T : class;
}
