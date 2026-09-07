using System;

namespace ViciOne.ServiceBus;

/// <summary>Overrides the broker entity name for the annotated message contract.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class EntityNameAttribute :
    Attribute
{
    /// <summary>Initializes the attribute with the specified broker entity name.</summary>
    /// <param name="entityName">The non-empty entity name to use for the message contract.</param>
    public EntityNameAttribute(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        EntityName = entityName;
    }

    /// <summary>Gets the declared broker entity name.</summary>
    public string EntityName { get; }
}
