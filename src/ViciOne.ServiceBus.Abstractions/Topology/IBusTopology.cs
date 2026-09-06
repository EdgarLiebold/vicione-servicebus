using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the operations required by bus topology.</summary>
public interface IBusTopology
{
    /// <summary>Gets the publish topology.</summary>
    IPublishTopology PublishTopology { get; }

    /// <summary>Gets the send topology.</summary>
    ISendTopology SendTopology { get; }

    /// <summary>Returns the publish topology for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message publish topology produced by the operation.</returns>
    IMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Returns the send topology for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message send topology produced by the operation.</returns>
    IMessageSendTopology<T> Send<T>()
        where T : class;

    /// <summary>Returns the message topology for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message topology produced by the operation.</returns>
    IMessageTopology<T> Message<T>()
        where T : class;

    /// <summary>Returns the destination address for the specified message type, as a short address.</summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress);

    /// <summary>Returns the destination address for the specified message type, as a short address.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class;
}
