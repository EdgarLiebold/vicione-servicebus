using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Composes conventions and explicit consume topology for one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class MessageConsumeTopology<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessageConsumeTopologyConvention<TMessage>> _conventions;
    readonly List<IMessageConsumeTopology<TMessage>> _delegateTopologies;
    readonly List<IMessageConsumeTopology<TMessage>> _topologies;

    /// <summary>Initializes empty topology for a message contract.</summary>
    public MessageConsumeTopology()
    {
        _conventions = new List<IMessageConsumeTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessageConsumeTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessageConsumeTopology<TMessage>>(8);
    }

    /// <summary>Gets whether the message contract may participate in consume bindings.</summary>
    protected bool IsBindableMessageType => GlobalTopology.IsConsumableMessageType(typeof(TMessage));

    /// <summary>Gets or sets whether the transport should configure consume topology for this contract.</summary>
    public bool ConfigureConsumeTopology { get; set; } = true;

    /// <summary>Adds explicit consume topology for this message contract.</summary>
    /// <param name="consumeTopology">The topology to apply after conventions.</param>
    public void Add(IMessageConsumeTopology<TMessage> consumeTopology)
    {
        ArgumentNullException.ThrowIfNull(consumeTopology);

        _topologies.Add(consumeTopology);
    }

    /// <summary>Adds delegated consume topology inherited from another contract.</summary>
    /// <param name="configuration">The delegated topology to apply first.</param>
    public void AddDelegate(IMessageConsumeTopology<TMessage> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _delegateTopologies.Add(configuration);
    }

    /// <summary>Applies delegated topology, conventions, and explicit topology in that order.</summary>
    /// <param name="builder">The consume-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (_delegateTopologies.Count > 0)
        {
            ITopologyPipeBuilder<ConsumeContext<TMessage>> delegatedBuilder = builder.CreateDelegatedBuilder();

            for (var index = 0; index < _delegateTopologies.Count; index++)
                _delegateTopologies[index].Apply(delegatedBuilder);
        }

        for (var index = 0; index < _conventions.Count; index++)
        {
            if (_conventions[index].TryGetMessageConsumeTopology(out IMessageConsumeTopology<TMessage>? topology))
                topology.Apply(builder);
        }

        for (var index = 0; index < _topologies.Count; index++)
            _topologies[index].Apply(builder);
    }

    /// <summary>Adds a message-specific convention unless its runtime type is already registered.</summary>
    /// <param name="convention">The convention to add.</param>
    /// <returns><see langword="true" /> when added; <see langword="false" /> for a duplicate runtime type.</returns>
    public bool TryAddConvention(IMessageConsumeTopologyConvention<TMessage> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        var conventionType = convention.GetType();

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i].GetType() == conventionType)
                return false;
        }

        _conventions.Add(convention);
        return true;
    }

    /// <summary>Replaces the existing convention of the requested type when present.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="update">Creates a replacement from the existing convention.</param>
    public void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>
    {
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention)
                    ?? throw new InvalidOperationException("The consume topology convention update returned null.");
                return;
            }
        }
    }

    /// <summary>Adds a convention or replaces the existing convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">Creates the convention when none exists.</param>
    /// <param name="update">Creates a replacement from the existing convention.</param>
    public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>
    {
        ArgumentNullException.ThrowIfNull(add);
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention)
                    ?? throw new InvalidOperationException("The consume topology convention update returned null.");
                return;
            }
        }

        TConvention addedConvention = add()
            ?? throw new InvalidOperationException("The consume topology convention factory returned null.");
        _conventions.Add(addedConvention);
    }

    /// <summary>Adds the message-specific convention exposed by a root consume convention.</summary>
    /// <param name="convention">The root convention to query.</param>
    /// <returns><see langword="true" /> when a convention is exposed and added; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IConsumeTopologyConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        return convention.TryGetMessageConsumeTopologyConvention(out IMessageConsumeTopologyConvention<TMessage>? messageConsumeTopologyConvention)
            && TryAddConvention(messageConsumeTopologyConvention);
    }

    /// <summary>Returns no failures because the transport-independent topology has no constraints of its own.</summary>
    /// <returns>An empty sequence.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }
}
