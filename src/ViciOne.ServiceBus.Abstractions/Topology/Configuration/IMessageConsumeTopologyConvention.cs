using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by message consume topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to get message consume topology.</summary>
    /// <param name="messageConsumeTopology">Receives the message consume topology produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology);
}


/// <summary>Defines the operations required by message consume topology convention.</summary>
public interface IMessageConsumeTopologyConvention
{
    /// <summary>Attempts to get message consume topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
        where T : class;
}
