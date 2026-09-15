using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Composes conventions and explicit send topology for one message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public class MessageSendTopology<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessageSendTopologyConvention<TMessage>> _conventions;
    readonly List<IMessageSendTopology<TMessage>> _delegateTopologies;
    readonly List<IMessageSendTopology<TMessage>> _topologies;

    /// <summary>Initializes empty topology for a message contract.</summary>
    public MessageSendTopology()
    {
        _conventions = new List<IMessageSendTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessageSendTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessageSendTopology<TMessage>>(8);
    }

    /// <summary>Adds explicit send topology for this message contract.</summary>
    /// <param name="sendTopology">The topology to apply after conventions.</param>
    public void Add(IMessageSendTopology<TMessage> sendTopology)
    {
        ArgumentNullException.ThrowIfNull(sendTopology);

        _topologies.Add(sendTopology);
    }

    /// <summary>Adds delegated send topology inherited from another contract.</summary>
    /// <param name="configuration">The delegated topology to apply first.</param>
    public void AddDelegate(IMessageSendTopology<TMessage> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _delegateTopologies.Add(configuration);
    }

    /// <summary>Applies delegated topology, conventions, and explicit topology in that order.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ITopologyPipeBuilder<SendContext<TMessage>> delegatedBuilder = builder.CreateDelegatedBuilder();

        for (var i = 0; i < _delegateTopologies.Count; i++)
            _delegateTopologies[i].Apply(delegatedBuilder);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i].TryGetMessageSendTopology(out IMessageSendTopology<TMessage>? topology))
                topology.Apply(builder);
        }

        for (var i = 0; i < _topologies.Count; i++)
            _topologies[i].Apply(builder);
    }

    /// <summary>Attempts to get a convention assignable to the requested type.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="convention">Receives the first matching convention when found.</param>
    /// <returns><see langword="true" /> when found; otherwise, <see langword="false" />.</returns>
    public bool TryGetConvention<TConvention>([NotNullWhen(true)] out TConvention? convention)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>
    {
        for (var i = 0; i < _conventions.Count; i++)
        {
            convention = _conventions[i] as TConvention;
            if (convention != null)
                return true;
        }

        convention = default;
        return false;
    }

    /// <summary>Adds a message-specific convention unless its runtime type is already registered.</summary>
    /// <param name="convention">The convention to add.</param>
    /// <returns><see langword="true" /> when added; <see langword="false" /> for a duplicate runtime type.</returns>
    public bool TryAddConvention(IMessageSendTopologyConvention<TMessage> convention)
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

    /// <summary>Adds the message-specific convention exposed by a root send convention.</summary>
    /// <param name="convention">The root convention to query.</param>
    /// <returns><see langword="true" /> when a convention is exposed and added; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(ISendTopologyConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        return convention.TryGetMessageSendTopologyConvention(out IMessageSendTopologyConvention<TMessage>? messageSendTopologyConvention)
            && TryAddConvention(messageSendTopologyConvention);
    }

    /// <summary>Replaces the existing convention of the requested type when present.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="update">Creates a replacement from the existing convention.</param>
    public void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>
    {
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention)
                    ?? throw new InvalidOperationException("The send topology convention update returned null.");
                return;
            }
        }
    }

    /// <summary>Adds a convention or replaces the existing convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">Creates the convention when none exists.</param>
    /// <param name="update">Creates a replacement from the existing convention.</param>
    public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>
    {
        ArgumentNullException.ThrowIfNull(add);
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention)
                    ?? throw new InvalidOperationException("The send topology convention update returned null.");
                return;
            }
        }

        TConvention addedConvention = add()
            ?? throw new InvalidOperationException("The send topology convention factory returned null.");
        _conventions.Add(addedConvention);
    }

    /// <summary>Returns no failures because the transport-independent topology has no constraints of its own.</summary>
    /// <returns>An empty sequence.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }
}
