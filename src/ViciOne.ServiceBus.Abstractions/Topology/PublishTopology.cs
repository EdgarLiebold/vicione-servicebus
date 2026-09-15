using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Owns message-specific publish topology, conventions, and publish-address resolution.</summary>
public class PublishTopology :
    IPublishTopologyConfigurator,
    IPublishTopologyConfigurationObserver
{
    readonly List<IMessagePublishTopologyConvention> _conventions;
    readonly object _lock = new();
    readonly ConcurrentDictionary<Type, Lazy<IMessagePublishTopologyConfigurator>> _messageTypes;
    readonly ConcurrentDictionary<Type, IMessageTypeSelector> _messageTypeSelectorCache;
    readonly Connectable<IPublishTopologyConfigurationObserver> _observers;

    /// <summary>Initializes an empty publish topology.</summary>
    public PublishTopology()
    {
        _messageTypes = new ConcurrentDictionary<Type, Lazy<IMessagePublishTopologyConfigurator>>();
        _messageTypeSelectorCache = new ConcurrentDictionary<Type, IMessageTypeSelector>();

        _conventions = new List<IMessagePublishTopologyConvention>(8);

        _observers = new Connectable<IPublishTopologyConfigurationObserver>();
        _observers.Connect(this);
    }

    void IPublishTopologyConfigurationObserver.MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
    {
        ApplyConventionsToMessageTopology(configurator);
    }

    IMessagePublishTopology<T> IPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    IMessagePublishTopologyConfigurator<T> IPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    /// <summary>Attempts to resolve the publish address for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="publishAddress">Receives the resolved address when one is available.</param>
    /// <returns><see langword="true" /> when the message topology resolves an address; otherwise, <see langword="false" />.</returns>
    public bool TryGetPublishAddress(Type messageType, Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(baseAddress);

        return GetMessageTopology(messageType).TryGetPublishAddress(baseAddress, out publishAddress);
    }

    /// <summary>Connects an observer for newly created publish-message topologies.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Adds a root publish convention unless its runtime type is already registered.</summary>
    /// <param name="convention">The convention to apply to current and future message topologies.</param>
    /// <returns><see langword="true" /> when added; <see langword="false" /> for a duplicate runtime type.</returns>
    public bool TryAddConvention(IPublishTopologyConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        var conventionType = convention.GetType();

        lock (_lock)
        {
            for (var i = 0; i < _conventions.Count; i++)
            {
                if (_conventions[i].GetType() == conventionType)
                    return false;
            }

            _conventions.Add(convention);
        }

        foreach (Lazy<IMessagePublishTopologyConfigurator> messagePublishTopologyConfigurator in _messageTypes.Values)
            messagePublishTopologyConfigurator.Value.TryAddConvention(convention);

        return true;
    }

    void IPublishTopologyConfigurator.AddMessagePublishTopology<T>(IMessagePublishTopology<T> topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        IMessagePublishTopologyConfigurator<T> messageConfiguration = GetMessageTopology<T>();

        messageConfiguration.Add(topology);
    }

    /// <summary>Validates every publish-message topology that has been created.</summary>
    /// <returns>The combined validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _messageTypes.Values.SelectMany(x => x.Value.Validate());
    }

    IMessagePublishTopology IPublishTopology.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType);
    }

    /// <summary>Gets or creates publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific publish topology configurator.</returns>
    public IMessagePublishTopologyConfigurator GetMessageTopology(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (MessageTypeCache.IsValidMessageType(messageType) == false)
            throw new ArgumentException(MessageTypeCache.InvalidMessageTypeReason(messageType), nameof(messageType));

        return _messageTypeSelectorCache.GetOrAdd(messageType, _ => Activation.Activate(messageType, new MessageTypeSelectorFactory(), this))
            .GetMessageTopology();
    }

    /// <summary>Creates publish topology for a message contract and links implemented contracts.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The created publish topology configurator.</returns>
    protected virtual IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
        where T : class
    {
        var messageTopology = new MessagePublishTopology<T>(this);

        var connector = new ImplementedMessageTypeConnector(this);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }

    /// <summary>Gets or creates publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The message-specific publish topology configurator.</returns>
    protected IMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        if (MessageTypeCache<T>.IsValidMessageType == false)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        Lazy<IMessagePublishTopologyConfigurator> topology =
            _messageTypes.GetOrAdd(typeof(T), _ => new Lazy<IMessagePublishTopologyConfigurator>(() => CreateMessageTopology<T>()));

        return (IMessagePublishTopologyConfigurator<T>)topology.Value;
    }

    /// <summary>Notifies observers about a newly created publish-message topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="messageTopology">The created topology configurator.</param>
    protected void OnMessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> messageTopology)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(messageTopology);

        _observers.ForEach(observer => observer.MessageTopologyCreated(messageTopology));
    }

    /// <summary>Invokes a callback for every publish-message topology that has been created.</summary>
    /// <typeparam name="T">The expected topology configurator type.</typeparam>
    /// <param name="callback">The callback to invoke.</param>
    protected void ForEachMessageType<T>(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        foreach (Lazy<IMessagePublishTopologyConfigurator> configurator in _messageTypes.Values)
            callback((T)configurator.Value);
    }

    void ApplyConventionsToMessageTopology<T>(IMessagePublishTopologyConfigurator<T> messageTopology)
        where T : class
    {
        IMessagePublishTopologyConvention[] conventions;
        lock (_lock)
            conventions = _conventions.ToArray();

        foreach (var convention in conventions)
        {
            if (convention.TryGetMessagePublishTopologyConvention(out IMessagePublishTopologyConvention<T>? messagePublishTopologyConvention))
                messageTopology.TryAddConvention(messagePublishTopologyConvention);
        }
    }


    class ImplementedMessageTypeConnector :
        IImplementedMessageType
    {
        readonly IPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IPublishTopologyConfigurator publishTopology)
        {
            _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            _publishTopology.GetMessageTopology<T>();
        }
    }


    readonly struct MessageTypeSelectorFactory :
        IActivationType<IMessageTypeSelector, PublishTopology>
    {
        public IMessageTypeSelector ActivateType<T>(PublishTopology publishTopology)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(publishTopology);

            return new MessageTypeSelector<T>(publishTopology);
        }
    }


    interface IMessageTypeSelector
    {
        IMessagePublishTopologyConfigurator GetMessageTopology();
    }


    class MessageTypeSelector<T> :
        IMessageTypeSelector
        where T : class
    {
        readonly PublishTopology _publishTopology;

        public MessageTypeSelector(PublishTopology publishTopology)
        {
            _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
        }

        public IMessagePublishTopologyConfigurator GetMessageTopology()
        {
            return _publishTopology.GetMessageTopology<T>();
        }
    }
}
