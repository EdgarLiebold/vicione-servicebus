using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Defines the contract for bus topology.
/// </summary>
public interface IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    IPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    ISendTopology SendTopology { get; }

    /// <summary>
    /// Returns the publish topology for the specified message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Returns the send topology for the specified message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessageSendTopology<T> Send<T>()
        where T : class;

    /// <summary>
    /// Returns the message topology for the specified message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessageTopology<T> Message<T>()
        where T : class;

    /// <summary>
    /// Returns the destination address for the specified message type, as a short address.
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="publishAddress"></param>
    /// <returns></returns>
    bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress);

    /// <summary>
    /// Returns the destination address for the specified message type, as a short address.
    /// </summary>
    /// <param name="publishAddress"></param>
    /// <returns></returns>
    bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class;
}
