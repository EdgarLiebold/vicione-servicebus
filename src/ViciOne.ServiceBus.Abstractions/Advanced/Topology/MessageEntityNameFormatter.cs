using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Resolves and caches the configured broker entity name for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public sealed class MessageEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly IEntityNameFormatter _entityNameFormatter;
    string? _entityName;
    readonly object _lock = new();

    /// <summary>Creates a formatter that honors message attributes before the fallback formatter.</summary>
    /// <param name="entityNameFormatter">The fallback formatter used when no attribute supplies a name.</param>
    public MessageEntityNameFormatter(IEntityNameFormatter entityNameFormatter)
    {
        _entityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));

        InitializeEntityNameFromAttributeIfSpecified();
    }

    /// <summary>Formats and caches the entity name for <typeparamref name="TMessage"/>.</summary>
    /// <returns>The configured entity name, or the name produced by the underlying formatter.</returns>
    public string FormatEntityName()
    {
        if (_entityName is not null)
            return _entityName;

        lock (_lock)
        {
            if (_entityName is not null)
                return _entityName;

            string entityName = _entityNameFormatter.FormatEntityName<TMessage>();
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName, "entityNameFormatter");
            return _entityName = entityName;
        }
    }

    void InitializeEntityNameFromAttributeIfSpecified()
    {
        var entityNameAttribute = typeof(TMessage).GetCustomAttribute<EntityNameAttribute>();
        if (entityNameAttribute != null)
            _entityName = entityNameAttribute.EntityName;
        else if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] messageTypes))
        {
            var faultEntityNameAttribute = messageTypes[0].GetCustomAttribute<FaultEntityNameAttribute>();
            if (faultEntityNameAttribute != null)
                _entityName = faultEntityNameAttribute.EntityName;
        }
    }
}
