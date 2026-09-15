using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Composes conventions and explicit publish topology for one message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public class MessagePublishTopology<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessagePublishTopologyConvention<TMessage>> _conventions;
    readonly List<IMessagePublishTopology<TMessage>> _delegateTopologies;
    readonly IPublishTopology _publishTopology;
    readonly List<IMessagePublishTopology<TMessage>> _topologies;
    bool? _exclude;

    /// <summary>Initializes topology for a message contract.</summary>
    /// <param name="publishTopology">The owning publish topology.</param>
    public MessagePublishTopology(IPublishTopology publishTopology)
    {
        _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
        _conventions = new List<IMessagePublishTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessagePublishTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessagePublishTopology<TMessage>>(8);
    }

    /// <summary>Gets or sets whether this message contract is excluded from publish topology.</summary>
    public bool Exclude
    {
        get => _exclude ??= IsMessageTypeExcluded();
        set => _exclude = value;
    }

    /// <summary>Adds explicit publish topology for this message contract.</summary>
    /// <param name="publishTopology">The topology to apply after conventions.</param>
    public void Add(IMessagePublishTopology<TMessage> publishTopology)
    {
        ArgumentNullException.ThrowIfNull(publishTopology);

        _topologies.Add(publishTopology);
    }

    /// <summary>Adds delegated publish topology inherited from another contract.</summary>
    /// <param name="configuration">The delegated topology to apply first.</param>
    public void AddDelegate(IMessagePublishTopology<TMessage> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _delegateTopologies.Add(configuration);
    }

    /// <summary>Applies delegated topology, conventions, and explicit topology in that order.</summary>
    /// <param name="builder">The publish-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ITopologyPipeBuilder<PublishContext<TMessage>> delegatedBuilder = builder.CreateDelegatedBuilder();

        for (var i = 0; i < _delegateTopologies.Count; i++)
            _delegateTopologies[i].Apply(delegatedBuilder);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i].TryGetMessagePublishTopology(out IMessagePublishTopology<TMessage>? topology))
                topology.Apply(builder);
        }

        foreach (IMessagePublishTopology<TMessage> topology in _topologies)
            topology.Apply(builder);
    }

    /// <summary>Attempts to resolve a provider-specific publish address.</summary>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="publishAddress">Receives the resolved address when one is available.</param>
    /// <returns><see langword="false" /> in the transport-independent topology.</returns>
    public virtual bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        publishAddress = null;
        return false;
    }

    /// <summary>Adds a message-specific convention unless its runtime type is already registered.</summary>
    /// <param name="convention">The convention to add.</param>
    /// <returns><see langword="true" /> when added; <see langword="false" /> for a duplicate runtime type.</returns>
    public bool TryAddConvention(IMessagePublishTopologyConvention<TMessage> convention)
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

    /// <summary>Adds the message-specific convention exposed by a root publish convention.</summary>
    /// <param name="convention">The root convention to query.</param>
    /// <returns><see langword="true" /> when a convention is exposed and added; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IPublishTopologyConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        return convention.TryGetMessagePublishTopologyConvention(out IMessagePublishTopologyConvention<TMessage>? messagePublishTopologyConvention)
            && TryAddConvention(messagePublishTopologyConvention);
    }

    /// <summary>Adds a convention or replaces the existing convention of the requested type.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">Creates the convention when none exists.</param>
    /// <param name="update">Creates a replacement from the existing convention.</param>
    public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessagePublishTopologyConvention<TMessage>
    {
        ArgumentNullException.ThrowIfNull(add);
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention)
                    ?? throw new InvalidOperationException("The publish topology convention update returned null.");
                return;
            }
        }

        TConvention addedConvention = add()
            ?? throw new InvalidOperationException("The publish topology convention factory returned null.");
        _conventions.Add(addedConvention);
    }

    /// <summary>Returns no failures because the transport-independent topology has no constraints of its own.</summary>
    /// <returns>An empty sequence.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    bool IsMessageTypeExcluded()
    {
        if (typeof(TMessage).GetCustomAttributes(typeof(ExcludeFromTopologyAttribute), false).Length > 0)
            return true;

        if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] types) && _publishTopology.GetMessageTopology(types[0]).Exclude)
            return true;

        return false;
    }
}
