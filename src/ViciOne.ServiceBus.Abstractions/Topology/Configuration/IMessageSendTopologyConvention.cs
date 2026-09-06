using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by message send topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to get message send topology.</summary>
    /// <param name="messageSendTopology">Receives the message send topology produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology);
}


/// <summary>Defines the operations required by message send topology convention.</summary>
public interface IMessageSendTopologyConvention
{
    /// <summary>Attempts to get message send topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class;
}
