using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Provides the message, send, and publish topology owned by one bus instance.</summary>
public interface IBusTopology
{
    /// <summary>Gets the publish topology.</summary>
    IPublishTopology PublishTopology { get; }

    /// <summary>Gets the send topology.</summary>
    ISendTopology SendTopology { get; }

    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract type.</typeparam>
    /// <returns>The publish topology for <typeparamref name="T" />.</returns>
    IMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Gets send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <returns>The send topology for <typeparamref name="T" />.</returns>
    IMessageSendTopology<T> Send<T>()
        where T : class;

    /// <summary>Gets shared entity-name topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The entity-name topology for <typeparamref name="T" />.</returns>
    IMessageTopology<T> Message<T>()
        where T : class;

    /// <summary>Attempts to resolve the publish address for a runtime message contract.</summary>
    /// <param name="messageType">The message contract type.</param>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when the message contract has a publish address; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress);

    /// <summary>Attempts to resolve the publish address for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when the message contract has a publish address; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class;
}
