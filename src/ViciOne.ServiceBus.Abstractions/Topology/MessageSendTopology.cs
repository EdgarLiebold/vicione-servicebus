using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageSendTopology<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessageSendTopologyConvention<TMessage>> _conventions;
    readonly List<IMessageSendTopology<TMessage>> _delegateTopologies;
    readonly List<IMessageSendTopology<TMessage>> _topologies;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageSendTopology()
    {
        _conventions = new List<IMessageSendTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessageSendTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessageSendTopology<TMessage>>(8);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="sendTopology">The send topology value.</param>
    public void Add(IMessageSendTopology<TMessage> sendTopology)
    {
        _topologies.Add(sendTopology);
    }

    /// <summary>
    /// Adds delegate to the configuration.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    public void AddDelegate(IMessageSendTopology<TMessage> configuration)
    {
        _delegateTopologies.Add(configuration);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
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

    /// <summary>
    /// Attempts to get convention.
    /// </summary>
    /// <typeparam name="TConvention">The t convention type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Performs the try add convention operation.
    /// </summary>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IMessageSendTopologyConvention<TMessage> convention)
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

    /// <summary>
    /// Performs the try add convention operation.
    /// </summary>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(ISendTopologyConvention convention)
    {
        return convention.TryGetMessageSendTopologyConvention(out IMessageSendTopologyConvention<TMessage>? messageSendTopologyConvention)
            && TryAddConvention(messageSendTopologyConvention);
    }

    /// <summary>
    /// Performs the update convention operation.
    /// </summary>
    /// <typeparam name="TConvention">The t convention type.</typeparam>
    /// <param name="update">The update value.</param>
    public void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>
    {
        for (var i = 0; i < _conventions.Count; i++)
        {
            if (_conventions[i] is TConvention convention)
            {
                _conventions[i] = update(convention);
                return;
            }
        }
    }

    /// <summary>
    /// Adds or update convention to the configuration.
    /// </summary>
    /// <typeparam name="TConvention">The t convention type.</typeparam>
    /// <param name="add">The add value.</param>
    /// <param name="update">The update value.</param>
    public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
        where TConvention : class, IMessageSendTopologyConvention<TMessage>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }
}
