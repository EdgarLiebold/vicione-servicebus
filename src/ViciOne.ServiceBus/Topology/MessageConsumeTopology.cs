using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Provides a message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageConsumeTopology<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IMessageConsumeTopologyConvention<TMessage>> _conventions;
    readonly List<IMessageConsumeTopology<TMessage>> _delegateTopologies;
    readonly List<IMessageConsumeTopology<TMessage>> _topologies;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageConsumeTopology()
    {
        _conventions = new List<IMessageConsumeTopologyConvention<TMessage>>(8);
        _topologies = new List<IMessageConsumeTopology<TMessage>>(8);
        _delegateTopologies = new List<IMessageConsumeTopology<TMessage>>(8);
    }

    /// <summary>
    /// Gets the is bindable message type value.
    /// </summary>
    protected bool IsBindableMessageType => GlobalTopology.IsConsumableMessageType(typeof(TMessage));

    /// <summary>
    /// Gets or sets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; set; } = true;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="consumeTopology">The consume topology value.</param>
    public void Add(IMessageConsumeTopology<TMessage> consumeTopology)
    {
        _topologies.Add(consumeTopology);
    }

    /// <summary>
    /// Adds delegate to the configuration.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    public void AddDelegate(IMessageConsumeTopology<TMessage> configuration)
    {
        _delegateTopologies.Add(configuration);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder)
    {
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

    /// <summary>
    /// Performs the try add convention operation.
    /// </summary>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IMessageConsumeTopologyConvention<TMessage> convention)
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
    /// Performs the update convention operation.
    /// </summary>
    /// <typeparam name="TConvention">The t convention type.</typeparam>
    /// <param name="update">The update value.</param>
    public void UpdateConvention<TConvention>(Func<TConvention, TConvention> update)
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>
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
        where TConvention : class, IMessageConsumeTopologyConvention<TMessage>
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
    /// Performs the try add convention operation.
    /// </summary>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IConsumeTopologyConvention convention)
    {
        return convention.TryGetMessageConsumeTopologyConvention(out IMessageConsumeTopologyConvention<TMessage>? messageConsumeTopologyConvention)
            && TryAddConvention(messageConsumeTopologyConvention);
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
