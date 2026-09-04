using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message consume topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention
    where TMessage : class
{
    /// <summary>
    /// Attempts to get message consume topology.
    /// </summary>
    /// <param name="messageConsumeTopology">The message consume topology value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology);
}


/// <summary>
/// Defines the contract for message consume topology convention.
/// </summary>
public interface IMessageConsumeTopologyConvention
{
    /// <summary>
    /// Attempts to get message consume topology convention.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
        where T : class;
}
