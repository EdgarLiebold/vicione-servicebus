using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Defines the topology for message publish.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessagePublishTopology<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessagePublishTopologyConvention<TMessage>> _conventions;
    readonly List<IMessagePublishTopology<TMessage>> _delegateTopologies;
    readonly IPublishTopology _publishTopology;
    readonly List<IMessagePublishTopology<TMessage>> _topologies;
    bool? _exclude;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    public MessagePublishTopology(IPublishTopology publishTopology)
    {
        _publishTopology = publishTopology;
        _conventions = new List<IMessagePublishTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessagePublishTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessagePublishTopology<TMessage>>(8);
    }

    /// <summary>Gets or sets the exclude.</summary>
    public bool Exclude
    {
        get => _exclude ??= IsMessageTypeExcluded();
        set => _exclude = value;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    public void Add(IMessagePublishTopology<TMessage> publishTopology)
    {
        _topologies.Add(publishTopology);
    }

    /// <summary>Adds delegate to the configuration.</summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    public void AddDelegate(IMessagePublishTopology<TMessage> configuration)
    {
        _delegateTopologies.Add(configuration);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
    {
        ITopologyPipeBuilder<PublishContext<TMessage>> delegatedBuilder = builder.CreateDelegatedBuilder();

        for (var i = 0; i < _delegateTopologies.Count; i++)
            _delegateTopologies[i].Apply(delegatedBuilder);

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i].TryGetMessagePublishTopology(out IMessagePublishTopology<TMessage> topology))
                topology.Apply(builder);
        }

        foreach (IMessagePublishTopology<TMessage> topology in _topologies)
            topology.Apply(builder);
    }

    /// <summary>Attempts to get publish address.</summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = null;
        return false;
    }

    /// <summary>Attempts to add convention.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IMessagePublishTopologyConvention<TMessage> convention)
    {
        var conventionType = convention.GetType();

        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i].GetType() == conventionType)
                return false;
        }

        _conventions.Add(convention);
        return true;
    }

    /// <summary>Attempts to add convention.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IPublishTopologyConvention convention)
    {
        return convention.TryGetMessagePublishTopologyConvention(out IMessagePublishTopologyConvention<TMessage> messagePublishTopologyConvention)
            && TryAddConvention(messagePublishTopologyConvention);
    }

    /// <summary>Adds or update convention to the configuration.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="add">The add.</param>
    /// <param name="update">The update.</param>
    public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessagePublishTopologyConvention<TMessage>
    {
        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention);
                return;
            }
        }

        var addedConvention = add();
        if (addedConvention != null)
            _conventions.Add(addedConvention);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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
