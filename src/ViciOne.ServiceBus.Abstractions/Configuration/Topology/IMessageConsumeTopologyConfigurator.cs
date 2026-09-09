using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consume topology for a message contract.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator,
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds another topology source for the same message contract.</summary>
    /// <param name="consumeTopology">The consume topology to compose.</param>
    void Add(IMessageConsumeTopology<TMessage> consumeTopology);

    /// <summary>
    /// Adds topology configuration that is applied before this configurator's own topology.
    /// </summary>
    /// <param name="configuration">The consume topology applied first.</param>
    void AddDelegate(IMessageConsumeTopology<TMessage> configuration);

    /// <summary>Adds a convention to this message contract's consume topology.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IMessageConsumeTopologyConvention<TMessage> convention);

    /// <summary>Replaces a registered convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="update">The function that produces the replacement convention.</param>
    void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>;

    /// <summary>
    /// Adds a convention when absent or replaces the first registered convention of the requested type.
    /// </summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="add">The factory used when no matching convention exists.</param>
    /// <param name="update">The function that replaces an existing convention.</param>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>;
}


/// <summary>Configures non-generic consume topology for a runtime message contract.</summary>
public interface IMessageConsumeTopologyConfigurator :
    ISpecification
{
    /// <summary>
    /// Gets or sets whether the transport creates consume topology for this message contract.
    /// </summary>
    bool ConfigureConsumeTopology { get; set; }

    /// <summary>Attempts to add a convention compatible with this runtime message contract.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IConsumeTopologyConvention convention);
}
