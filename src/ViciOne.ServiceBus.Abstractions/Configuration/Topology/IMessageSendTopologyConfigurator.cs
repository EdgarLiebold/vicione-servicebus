using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures send topology for a message contract.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator,
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds another topology source for the same message contract.</summary>
    /// <param name="sendTopology">The send topology to compose.</param>
    void Add(IMessageSendTopology<TMessage> sendTopology);

    /// <summary>
    /// Adds topology configuration that is applied before this configurator's own topology.
    /// </summary>
    /// <param name="configuration">The send topology applied first.</param>
    void AddDelegate(IMessageSendTopology<TMessage> configuration);

    /// <summary>Adds a convention to this message contract's send topology.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IMessageSendTopologyConvention<TMessage> convention);

    /// <summary>Replaces a registered convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="update">The function that produces the replacement convention.</param>
    void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;

    /// <summary>
    /// Adds a convention when absent or replaces the first registered convention of the requested type.
    /// </summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="add">The factory used when no matching convention exists.</param>
    /// <param name="update">The function that replaces an existing convention.</param>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;

    /// <summary>Attempts to get the first registered convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="convention">Receives the matching convention when found.</param>
    /// <returns><see langword="true" /> when a matching convention was found; otherwise, <see langword="false" />.</returns>
    bool TryGetConvention<TConvention>([NotNullWhen(true)] out TConvention? convention)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>;
}


/// <summary>Configures non-generic send topology for a runtime message contract.</summary>
public interface IMessageSendTopologyConfigurator :
    ISpecification
{
    /// <summary>Attempts to add a convention compatible with this runtime message contract.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(ISendTopologyConvention convention);
}
