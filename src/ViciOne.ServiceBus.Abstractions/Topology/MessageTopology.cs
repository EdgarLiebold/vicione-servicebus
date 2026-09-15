using System;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Creates and caches topology for every valid message contract.</summary>
public class MessageTopology :
    IMessageTopologyConfigurator
{
    readonly ConcurrentDictionary<Type, object> _messageTypes;
    readonly Connectable<IMessageTopologyConfigurationObserver> _observers;

    /// <summary>Initializes the topology with its default entity-name formatter.</summary>
    /// <param name="entityNameFormatter">The formatter used by newly created message topologies.</param>
    public MessageTopology(IEntityNameFormatter entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));

        _messageTypes = new ConcurrentDictionary<Type, object>();
        _observers = new Connectable<IMessageTopologyConfigurationObserver>();
    }

    /// <summary>Gets the formatter used by newly created message topologies.</summary>
    public IEntityNameFormatter EntityNameFormatter { get; private set; }

    /// <summary>Replaces the formatter used by subsequently created message topologies.</summary>
    /// <param name="entityNameFormatter">The replacement formatter.</param>
    public void SetEntityNameFormatter(IEntityNameFormatter entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));
    }

    IMessageTopologyConfigurator<T> IMessageTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    /// <summary>Connects an observer for newly created message topologies.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    IMessageTopology<T> IMessageTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    IMessageTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        if (MessageTypeCache<T>.IsValidMessageType == false)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var specification = _messageTypes.GetOrAdd(typeof(T), _ => CreateMessageTopology<T>());

        return (IMessageTopologyConfigurator<T>)specification;
    }

    /// <summary>Creates topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The created message topology configurator.</returns>
    protected virtual IMessageTopologyConfigurator<T> CreateMessageTopology<T>()
        where T : class
    {
        var messageTopology = new MessageTopology<T>(new MessageEntityNameFormatter<T>(EntityNameFormatter));

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }

    void OnMessageTopologyCreated<T>(IMessageTopologyConfigurator<T> messageTopology)
        where T : class
    {
        _observers.ForEach(observer => observer.MessageTopologyCreated(messageTopology));
    }
}


/// <summary>Owns entity-name configuration for one message contract.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public class MessageTopology<TMessage> :
    IMessageTopologyConfigurator<TMessage>
    where TMessage : class
{
    string? _entityName;
    readonly object _lock = new();

    /// <summary>Initializes topology with an entity-name formatter.</summary>
    /// <param name="entityNameFormatter">The formatter for this message contract.</param>
    public MessageTopology(IMessageEntityNameFormatter<TMessage> entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));
    }

    /// <summary>Gets the formatter for this message contract.</summary>
    public IMessageEntityNameFormatter<TMessage> EntityNameFormatter { get; private set; }

    /// <summary>Gets the entity name, evaluating the formatter at most once.</summary>
    public string EntityName
    {
        get
        {
            if (_entityName is not null)
                return _entityName;

            lock (_lock)
            {
                if (_entityName is not null)
                    return _entityName;

                string entityName = EntityNameFormatter.FormatEntityName();
                ArgumentException.ThrowIfNullOrWhiteSpace(entityName, nameof(EntityNameFormatter));
                _entityName = entityName;
                return entityName;
            }
        }
    }

    /// <summary>Replaces the formatter before the entity name is first evaluated.</summary>
    /// <param name="entityNameFormatter">The replacement formatter.</param>
    public void SetEntityNameFormatter(IMessageEntityNameFormatter<TMessage> entityNameFormatter)
    {
        ArgumentNullException.ThrowIfNull(entityNameFormatter);

        lock (_lock)
        {
            if (_entityName != null)
            {
                if (_entityName == entityNameFormatter.FormatEntityName())
                    return;

                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Topology", "unknown", $"The message type {TypeCache<TMessage>.ShortName} entity name was already evaluated: {_entityName}", "Correct the named configuration before starting the host"));
            }

            EntityNameFormatter = entityNameFormatter;
        }
    }

    /// <summary>Sets a fixed entity name before the current name is first evaluated.</summary>
    /// <param name="entityName">The non-empty entity name.</param>
    public void SetEntityName(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        SetEntityNameFormatter(new StaticEntityNameFormatter<TMessage>(entityName));
    }
}
