using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the Publishing of a message type, allowing filters to be applied
/// on Publish.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator,
    IMessagePublishTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    void Add(IMessagePublishTopology<TMessage> publishTopology);

    /// <summary>
    /// Adds a delegated configuration to the Publish topology, which is called before any topologies
    /// in this configuration.
    /// </summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    void AddDelegate(IMessagePublishTopology<TMessage> configuration);

    /// <summary>Adds a convention to the message Publish topology configuration, which can be modified.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IMessagePublishTopologyConvention<TMessage> convention);

    /// <summary>
    /// Returns the first convention that matches the interface type specified, to allow it to be customized
    /// and or replaced.
    /// </summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">Called if the convention does not already exist.</param>
    /// <param name="update">Called if the convention already exists.</param>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessagePublishTopologyConvention<TMessage>;
}


/// <summary>Configures message publish topology.</summary>
public interface IMessagePublishTopologyConfigurator :
    IMessagePublishTopology,
    ISpecification
{
    /// <summary>Exclude the message type from being created as a topic/exchange.</summary>
    new bool Exclude { set; }

    /// <summary>Attempts to add convention.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IPublishTopologyConvention convention);
}
