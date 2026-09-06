using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the topology for consume.</summary>
public class ConsumeTopology :
    IConsumeTopologyConfigurator,
    IConsumeTopologyConfigurationObserver
{
    readonly List<IMessageConsumeTopologyConvention> _conventions;
    readonly object _lock = new();
    readonly int _maxQueueNameLength;
    readonly ConcurrentDictionary<Type, Lazy<IMessageConsumeTopologyConfigurator>> _messageTypes;
    readonly ConcurrentDictionary<Type, IMessageTypeSelector> _messageTypeSelectorCache;
    readonly ConsumeTopologyConfigurationObservable _observers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="maxQueueNameLength">The max queue name length.</param>
    protected ConsumeTopology(int maxQueueNameLength = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueueNameLength, EntityNameShortener.MinimumMaximumLength);

        _maxQueueNameLength = maxQueueNameLength;

        _messageTypes = new ConcurrentDictionary<Type, Lazy<IMessageConsumeTopologyConfigurator>>();
        _messageTypeSelectorCache = new ConcurrentDictionary<Type, IMessageTypeSelector>();

        _conventions = new List<IMessageConsumeTopologyConvention>(8);

        _observers = new ConsumeTopologyConfigurationObservable();
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

    /// <summary>Gets message topology.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message topology.</returns>
    public IMessageConsumeTopologyConfigurator GetMessageTopology(Type messageType)
    {
        return _messageTypeSelectorCache.GetOrAdd(messageType, _ => Activation.Activate(messageType, new MessageTypeSelectorFactory(), this))
            .GetMessageTopology();
    }

    /// <summary>Creates temporary queue name.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The created temporary queue name.</returns>
    public virtual string CreateTemporaryQueueName(string tag)
    {
        return EntityNameShortener.Shorten(DefaultEndpointNameFormatter.GetTemporaryQueueName(tag), _maxQueueNameLength);
    }

    /// <summary>Connects consume topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Attempts to add convention.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAddConvention(IConsumeTopologyConvention convention)
    {
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

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _messageTypes.Values.SelectMany(x => x.Value.Validate());
    }

    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
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

    /// <summary>Returns every available value.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected bool All(Func<IMessageConsumeTopologyConfigurator, bool> callback)
    {
        IMessageConsumeTopologyConfigurator[] configurators;
        lock (_lock)
            configurators = _messageTypes.Values.Select(x => x.Value).ToArray();

        if (configurators.Length == 0)
            return true;

        if (configurators.Length == 1)
            return callback(configurators[0]);

        return configurators.All(callback);
    }

    /// <summary>Selects many.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="selector">The selector.</param>
    /// <returns>The selected many.</returns>
    protected IEnumerable<TResult> SelectMany<T, TResult>(Func<T, IEnumerable<TResult>> selector)
        where T : class
    {
        IMessageConsumeTopologyConfigurator[] configurators;
        lock (_lock)
            configurators = _messageTypes.Values.Select(x => x.Value).ToArray();

        if (configurators.Length == 0)
            return Enumerable.Empty<TResult>();

        if (configurators.Length == 1)
            return selector((T)configurators[0]);

        return configurators.Cast<T>().SelectMany(selector);
    }

    /// <summary>Applies the callback to every value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    protected void ForEach<T>(Action<T> callback)
        where T : class
    {
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

    /// <summary>Creates message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created message topology.</returns>
    protected virtual IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
        where T : class
    {
        var messageTopology = new MessageConsumeTopology<T>();

        OnMessageTopologyCreated(messageTopology);
        return messageTopology;
    }

    /// <summary>Reports that on message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="messageTopology">The message topology.</param>
    protected void OnMessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> messageTopology)
        where T : class
    {
        _observers.MessageTopologyCreated(messageTopology);
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
            _consumeTopology = consumeTopology;
        }

        public IMessageConsumeTopologyConfigurator GetMessageTopology()
        {
            return _consumeTopology.GetMessageTopology<T>();
        }
    }
}
