using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats message entity name values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly IEntityNameFormatter _entityNameFormatter;
    string? _entityName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    public MessageEntityNameFormatter(IEntityNameFormatter entityNameFormatter)
    {
        _entityNameFormatter = entityNameFormatter;

        InitializeEntityNameFromAttributeIfSpecified();
    }

    /// <summary>Formats and caches the entity name for <typeparamref name="TMessage"/>.</summary>
    /// <returns>The configured entity name, or the name produced by the underlying formatter.</returns>
    public string FormatEntityName()
    {
        return _entityName ??= _entityNameFormatter.FormatEntityName<TMessage>();
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
