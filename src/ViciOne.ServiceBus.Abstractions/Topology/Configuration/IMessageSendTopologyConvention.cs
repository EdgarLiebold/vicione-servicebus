using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention
    where TMessage : class
{
    /// <summary>
    /// Attempts to get message send topology.
    /// </summary>
    /// <param name="messageSendTopology">The message send topology value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology);
}


/// <summary>
/// Defines the contract for message send topology convention.
/// </summary>
public interface IMessageSendTopologyConvention
{
    /// <summary>
    /// Attempts to get message send topology convention.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class;
}
