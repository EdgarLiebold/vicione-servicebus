using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Owns message-specific consume topology, conventions, and configuration observers for one receive endpoint.</summary>
public class ConsumeTopology :
    IConsumeTopologyConfigurator,
    IConsumeTopologyConfigurationObserver
{
    readonly List<IMessageConsumeTopologyConvention> _conventions;
    readonly object _lock = new();
    readonly int _maxQueueNameLength;
    readonly ConcurrentDictionary<Type, Lazy<IMessageConsumeTopologyConfigurator>> _messageTypes;
    readonly ConcurrentDictionary<Type, IMessageTypeSelector> _messageTypeSelectorCache;
    readonly Connectable<IConsumeTopologyConfigurationObserver> _observers;

    /// <summary>Creates an empty consume topology with bounded generated queue names.</summary>
    /// <param name="maxQueueNameLength">The maximum length of a generated temporary queue name.</param>
    protected ConsumeTopology(int maxQueueNameLength = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueueNameLength, EntityNameShortener.MinimumMaximumLength);

        _maxQueueNameLength = maxQueueNameLength;

        _messageTypes = new ConcurrentDictionary<Type, Lazy<IMessageConsumeTopologyConfigurator>>();
        _messageTypeSelectorCache = new ConcurrentDictionary<Type, IMessageTypeSelector>();

        _conventions = new List<IMessageConsumeTopologyConvention>(8);

        _observers = new Connectable<IConsumeTopologyConfigurationObserver>();
        _observers.Connect(this);
    }

    void IConsumeTopologyConfigurationObserver.MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> messageTopology)
    {
        ApplyConventionsToMessageTopology(messageTopology);
    }

    IMessageConsumeTopology<T> IConsumeTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    IMessageConsumeTopologyConfigurator<T> IConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    /// <summary>Gets or creates the consume topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific consume topology configurator.</returns>
    public IMessageConsumeTopologyConfigurator GetMessageTopology(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (MessageTypeCache.IsValidMessageType(messageType) == false)
            throw new ArgumentException(MessageTypeCache.InvalidMessageTypeReason(messageType), nameof(messageType));

        return _messageTypeSelectorCache.GetOrAdd(messageType, _ => Activation.Activate(messageType, new MessageTypeSelectorFactory(), this))
            .GetMessageTopology();
    }

    /// <summary>Creates a transport-safe temporary queue name within the configured length bound.</summary>
    /// <param name="tag">The non-empty diagnostic tag included in the generated name.</param>
    /// <returns>The bounded temporary queue name.</returns>
    public virtual string CreateTemporaryQueueName(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return EntityNameShortener.Shorten(DefaultEndpointNameFormatter.GetTemporaryQueueName(tag), _maxQueueNameLength);
    }

    /// <summary>Connects an observer for newly created consume-message topologies.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Adds a convention unless another convention with the same runtime type is already registered.</summary>
    /// <param name="convention">The convention to register and apply to existing message topologies.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IConsumeTopologyConvention convention)
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

        foreach (Lazy<IMessageConsumeTopologyConfigurator> messageConsumeTopologyConfigurator in _messageTypes.Values)
            messageConsumeTopologyConfigurator.Value.TryAddConvention(convention);

        return true;
    }

    /// <summary>Validates every consume-message topology that has been created.</summary>
    /// <returns>The combined validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _messageTypes.Values.SelectMany(x => x.Value.Validate());
    }

    /// <summary>Gets or creates consume topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The message-specific consume topology configurator.</returns>
    protected IMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        if (MessageTypeCache<T>.IsValidMessageType == false)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        Lazy<IMessageConsumeTopologyConfigurator> specification = _messageTypes.GetOrAdd(typeof(T),
            _ => new Lazy<IMessageConsumeTopologyConfigurator>(() => CreateMessageTopology<T>()));

        return specification.Value as IMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The consume topology for {TypeCache<T>.ShortName} has an incompatible type.");
    }

    /// <summary>Determines whether a predicate accepts every created consume-message topology.</summary>
    /// <param name="callback">The predicate to evaluate.</param>
    /// <returns><see langword="true" /> when every topology is accepted, including when none exist; otherwise, <see langword="false" />.</returns>
    protected bool All(Func<IMessageConsumeTopologyConfigurator, bool> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        IMessageConsumeTopologyConfigurator[] configurators;
        lock (_lock)
            configurators = _messageTypes.Values.Select(x => x.Value).ToArray();

        if (configurators.Length == 0)
            return true;

        if (configurators.Length == 1)
            return callback(configurators[0]);

        return configurators.All(callback);
    }

    /// <summary>Projects and flattens values from every created consume-message topology.</summary>
    /// <typeparam name="T">The expected topology configurator type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="selector">The projection to apply to each topology.</param>
    /// <returns>The flattened projected values.</returns>
    protected IEnumerable<TResult> SelectMany<T, TResult>(Func<T, IEnumerable<TResult>> selector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(selector);

        IMessageConsumeTopologyConfigurator[] configurators;
        lock (_lock)
            configurators = _messageTypes.Values.Select(x => x.Value).ToArray();

        if (configurators.Length == 0)
            return Enumerable.Empty<TResult>();

        if (configurators.Length == 1)
            return selector((T)configurators[0]);

        return configurators.Cast<T>().SelectMany(selector);
    }

    /// <summary>Invokes a callback for every created consume-message topology.</summary>
    /// <typeparam name="T">The expected topology configurator type.</typeparam>
    /// <param name="callback">The callback to invoke.</param>
    protected void ForEach<T>(Action<T> callback)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(callback);

        IMessageConsumeTopologyConfigurator[] configurators;
        lock (_lock)
            configurators = _messageTypes.Values.Select(x => x.Value).ToArray();

        switch (configurators.Length)
        {
            case 0:
                break;
            case 1:
                callback((T)configurators[0]);
                break;
            default:
                foreach (var configurator in configurators.Cast<T>())
                    callback(configurator);

                break;
        }
    }

    /// <summary>Creates consume topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The created consume topology configurator.</returns>
    protected virtual IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
        where T : class
    {
        var messageTopology = new MessageConsumeTopology<T>();

        OnMessageTopologyCreated(messageTopology);
        return messageTopology;
    }

    /// <summary>Notifies observers about a newly created consume-message topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="messageTopology">The created topology configurator.</param>
    protected void OnMessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> messageTopology)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(messageTopology);

        _observers.ForEach(observer => observer.MessageTopologyCreated(messageTopology));
    }

    void ApplyConventionsToMessageTopology<T>(IMessageConsumeTopologyConfigurator<T> messageTopology)
        where T : class
    {
        IMessageConsumeTopologyConvention[] conventions;
        lock (_lock)
            conventions = _conventions.ToArray();

        foreach (var convention in conventions)
        {
            if (convention.TryGetMessageConsumeTopologyConvention(out IMessageConsumeTopologyConvention<T>? messageConsumeTopologyConvention))
                messageTopology.TryAddConvention(messageConsumeTopologyConvention);
        }
    }


    readonly struct MessageTypeSelectorFactory :
        IActivationType<IMessageTypeSelector, ConsumeTopology>
    {
        public IMessageTypeSelector ActivateType<T>(ConsumeTopology consumeTopology)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(consumeTopology);

            return new MessageTypeSelector<T>(consumeTopology);
        }
    }


    interface IMessageTypeSelector
    {
        IMessageConsumeTopologyConfigurator GetMessageTopology();
    }


    class MessageTypeSelector<T> :
        IMessageTypeSelector
        where T : class
    {
        readonly ConsumeTopology _consumeTopology;

        public MessageTypeSelector(ConsumeTopology consumeTopology)
        {
            _consumeTopology = consumeTopology ?? throw new ArgumentNullException(nameof(consumeTopology));
        }

        public IMessageConsumeTopologyConfigurator GetMessageTopology()
        {
            return _consumeTopology.GetMessageTopology<T>();
        }
    }
}
