using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures publish topology for a message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator,
    IMessagePublishTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds another topology source for the same message contract.</summary>
    /// <param name="publishTopology">The publish topology to compose.</param>
    void Add(IMessagePublishTopology<TMessage> publishTopology);

    /// <summary>
    /// Adds topology configuration that is applied before this configurator's own topology.
    /// </summary>
    /// <param name="configuration">The publish topology applied first.</param>
    void AddDelegate(IMessagePublishTopology<TMessage> configuration);

    /// <summary>Adds a convention to this message contract's publish topology.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IMessagePublishTopologyConvention<TMessage> convention);

    /// <summary>
    /// Adds a convention when absent or replaces the first registered convention of the requested type.
    /// </summary>
    /// <typeparam name="TConvention">The convention contract.</typeparam>
    /// <param name="add">The factory used when no matching convention exists.</param>
    /// <param name="update">The function that replaces an existing convention.</param>
    void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessagePublishTopologyConvention<TMessage>;
}


/// <summary>Configures non-generic publish topology for a runtime message contract.</summary>
public interface IMessagePublishTopologyConfigurator :
    IMessagePublishTopology,
    ISpecification
{
    /// <summary>Sets whether the transport omits a broker topic or exchange for this message contract.</summary>
    new bool Exclude { set; }

    /// <summary>Attempts to add a convention compatible with this runtime message contract.</summary>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IPublishTopologyConvention convention);
}
