using System;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Defines the topology for message.</summary>
public class MessageTopology :
    IMessageTopologyConfigurator
{
    readonly ConcurrentDictionary<Type, IMessageTypeTopologyConfigurator> _messageTypes;
    readonly MessageTopologyConfigurationObservable _observers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    public MessageTopology(IEntityNameFormatter entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));

        _messageTypes = new ConcurrentDictionary<Type, IMessageTypeTopologyConfigurator>();
        _observers = new MessageTopologyConfigurationObservable();
    }

    /// <summary>Gets or sets the entity name formatter.</summary>
    public IEntityNameFormatter EntityNameFormatter { get; private set; }

    /// <summary>Sets entity name formatter.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    public void SetEntityNameFormatter(IEntityNameFormatter entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));
    }

    IMessageTopologyConfigurator<T> IMessageTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>();
    }

    /// <summary>Connects message topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer)
    {
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

        var specification = _messageTypes.GetOrAdd(typeof(T), CreateMessageTopology<T>);

        return (IMessageTopologyConfigurator<T>)specification;
    }

    /// <summary>Creates message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns>The created message topology.</returns>
    protected virtual IMessageTypeTopologyConfigurator CreateMessageTopology<T>(Type type)
        where T : class
    {
        var messageTopology = new MessageTopology<T>(new MessageEntityNameFormatter<T>(EntityNameFormatter));

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }

    void OnMessageTopologyCreated<T>(IMessageTopologyConfigurator<T> messageTopology)
        where T : class
    {
        _observers.MessageTopologyCreated(messageTopology);
    }
}


/// <summary>Defines the topology for message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageTopology<TMessage> :
    IMessageTopologyConfigurator<TMessage>
    where TMessage : class
{
    string? _entityName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    public MessageTopology(IMessageEntityNameFormatter<TMessage> entityNameFormatter)
    {
        EntityNameFormatter = entityNameFormatter;
    }

    /// <summary>Gets or sets the entity name formatter.</summary>
    public IMessageEntityNameFormatter<TMessage> EntityNameFormatter { get; private set; }

    /// <summary>Gets the entity name.</summary>
    public string EntityName => _entityName ??= EntityNameFormatter.FormatEntityName();

    /// <summary>Sets entity name formatter.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    public void SetEntityNameFormatter(IMessageEntityNameFormatter<TMessage> entityNameFormatter)
    {
        if (entityNameFormatter == null)
            throw new ArgumentNullException(nameof(entityNameFormatter));

        if (_entityName != null)
        {
            if (_entityName == entityNameFormatter.FormatEntityName())
                return;

            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Topology", "unknown", $"The message type {TypeCache<TMessage>.ShortName} entity name was already evaluated: {_entityName}", "Correct the named configuration before starting the host"));
        }

        EntityNameFormatter = entityNameFormatter;
    }

    /// <summary>Sets entity name.</summary>
    /// <param name="entityName">The entity name.</param>
    public void SetEntityName(string entityName)
    {
        if (entityName == null)
            throw new ArgumentNullException(nameof(entityName));

        SetEntityNameFormatter(new StaticEntityNameFormatter<TMessage>(entityName));
    }
}
