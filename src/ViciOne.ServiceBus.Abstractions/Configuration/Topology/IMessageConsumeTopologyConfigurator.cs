using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the Consuming of a message type, allowing filters to be applied
/// on Consume.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator,
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="consumeTopology">The consume topology value.</param>
    void Add(IMessageConsumeTopology<TMessage> consumeTopology);

    /// <summary>
    /// Adds a delegated configuration to the Consume topology, which is called before any topologies
    /// in this configuration.
    /// </summary>
    /// <param name="configuration"></param>
    void AddDelegate(IMessageConsumeTopology<TMessage> configuration);

    /// <summary>
    /// Adds a convention to the message Consume topology configuration, which can be modified
    /// </summary>
    /// <param name="convention"></param>
    bool TryAddConvention(IMessageConsumeTopologyConvention<TMessage> convention);

    /// <summary>
    /// Update a convention if available, otherwise, throw an exception
    /// </summary>
    /// <typeparam name="TConvention"></typeparam>
    /// <param name="update">Called if the convention already exists</param>
    /// <returns></returns>
    void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>;

    /// <summary>
    /// Returns the first convention that matches the interface type specified, to allow it to be customized
    /// and or replaced.
    /// </summary>
    /// <typeparam name="TConvention"></typeparam>
    /// <param name="add">Called if the convention does not already exist</param>
    /// <param name="update">Called if the convention already exists</param>
    /// <returns></returns>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>;
}


/// <summary>
/// Defines the contract for message consume topology configurator.
/// </summary>
public interface IMessageConsumeTopologyConfigurator :
    ISpecification
{
    /// <summary>
    /// Specify whether the broker topology should be configured for this message type
    /// (defaults to true)
    /// </summary>
    bool ConfigureConsumeTopology { get; set; }

    /// <summary>
    /// Performs the try add convention operation.
    /// </summary>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IConsumeTopologyConvention convention);
}
