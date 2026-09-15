using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Owns message-specific send topology, conventions, and failure-queue naming.</summary>
public class SendTopology :
    ISendTopologyConfigurator,
    ISendTopologyConfigurationObserver
{
    readonly List<IMessageSendTopologyConvention> _conventions;
    IDeadLetterQueueNameFormatter _deadLetterQueueNameFormatter;
    IErrorQueueNameFormatter _errorQueueNameFormatter;
    readonly object _lock = new();
    readonly ConcurrentDictionary<Type, Lazy<IMessageSendTopologyConfigurator>> _messageTypes;
    readonly Connectable<ISendTopologyConfigurationObserver> _observers;

    /// <summary>Initializes an empty send topology with the default failure-queue formatters.</summary>
    public SendTopology()
    {
        _messageTypes = new ConcurrentDictionary<Type, Lazy<IMessageSendTopologyConfigurator>>();

        _observers = new Connectable<ISendTopologyConfigurationObserver>();

        _conventions = new List<IMessageSendTopologyConvention>(8);

        _deadLetterQueueNameFormatter = DefaultDeadLetterQueueNameFormatter.Instance;
        _errorQueueNameFormatter = DefaultErrorQueueNameFormatter.Instance;

        _observers.Connect(this);
    }

    void ISendTopologyConfigurationObserver.MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> messageTopology)
    {
        ApplyConventionsToMessageTopology(messageTopology);
    }

    /// <summary>Gets or sets the formatter for dead-letter queue names.</summary>
    public IDeadLetterQueueNameFormatter DeadLetterQueueNameFormatter
    {
        get => _deadLetterQueueNameFormatter;
        set => _deadLetterQueueNameFormatter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the formatter for error queue names.</summary>
    public IErrorQueueNameFormatter ErrorQueueNameFormatter
    {
        get => _errorQueueNameFormatter;
        set => _errorQueueNameFormatter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or creates send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The message-specific send topology configurator.</returns>
    public IMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        if (MessageTypeCache<T>.IsValidMessageType == false)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        Lazy<IMessageSendTopologyConfigurator>? specification = _messageTypes.GetOrAdd(typeof(T),
            _ => new Lazy<IMessageSendTopologyConfigurator>(() => CreateMessageTopology<T>()));

        return (IMessageSendTopologyConfigurator<T>)specification.Value;
    }

    /// <summary>Connects an observer for newly created send-message topologies.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Adds a root send convention unless its runtime type is already registered.</summary>
    /// <param name="convention">The convention to apply to current and future message topologies.</param>
    /// <returns><see langword="true" /> when added; <see langword="false" /> for a duplicate runtime type.</returns>
    public bool TryAddConvention(ISendTopologyConvention convention)
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

        foreach (Lazy<IMessageSendTopologyConfigurator> messageSendTopologyConfigurator in _messageTypes.Values)
            messageSendTopologyConfigurator.Value.TryAddConvention(convention);

        return true;
    }

    void ISendTopologyConfigurator.AddMessageSendTopology<T>(IMessageSendTopology<T> topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        IMessageSendTopologyConfigurator<T> messageConfiguration = GetMessageTopology<T>();

        messageConfiguration.Add(topology);
    }

    /// <summary>Validates every send-message topology that has been created.</summary>
    /// <returns>The combined validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _messageTypes.Values.SelectMany(x => x.Value.Validate());
    }

    /// <summary>Creates send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The created send topology configurator.</returns>
    protected virtual IMessageSendTopologyConfigurator CreateMessageTopology<T>()
        where T : class
    {
        var messageTopology = new MessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);
        return messageTopology;
    }

    /// <summary>Notifies observers about a newly created send-message topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="messageTopology">The created topology configurator.</param>
    protected void OnMessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> messageTopology)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(messageTopology);

        _observers.ForEach(observer => observer.MessageTopologyCreated(messageTopology));
    }

    void ApplyConventionsToMessageTopology<T>(IMessageSendTopologyConfigurator<T> messageTopology)
        where T : class
    {
        IMessageSendTopologyConvention[] conventions;
        lock (_lock)
            conventions = _conventions.ToArray();

        foreach (var convention in conventions)
        {
            if (convention.TryGetMessageSendTopologyConvention(out IMessageSendTopologyConvention<T>? messageSendTopologyConvention))
                messageTopology.TryAddConvention(messageSendTopologyConvention);
        }
    }
}
