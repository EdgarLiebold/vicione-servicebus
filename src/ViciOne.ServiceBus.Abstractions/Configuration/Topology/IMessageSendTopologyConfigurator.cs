using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the sending of a message type, allowing filters to be applied
/// on send.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator,
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="sendTopology">The send topology.</param>
    void Add(IMessageSendTopology<TMessage> sendTopology);

    /// <summary>
    /// Adds a delegated configuration to the send topology, which is called before any topologies
    /// in this configuration.
    /// </summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    void AddDelegate(IMessageSendTopology<TMessage> configuration);

    /// <summary>Adds a convention to the message send topology configuration, which can be modified.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IMessageSendTopologyConvention<TMessage> convention);

    /// <summary>Update a convention if available, otherwise, throw an exception.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="update">Called if the convention already exists.</param>
    void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;

    /// <summary>
    /// Returns the first convention that matches the interface type specified, to allow it to be customized
    /// and or replaced.
    /// </summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">Called if the convention does not already exist.</param>
    /// <param name="update">Called if the convention already exists.</param>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;

    /// <summary>Returns the convention, if found.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetConvention<TConvention>([NotNullWhen(true)] out TConvention? convention)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;
}


/// <summary>Configures message send topology.</summary>
public interface IMessageSendTopologyConfigurator :
    ISpecification
{
    /// <summary>Attempts to add convention.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(ISendTopologyConvention convention);
}
