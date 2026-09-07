using System;

namespace ViciOne.ServiceBus;

/// <summary>Overrides the broker entity name used for faults of the annotated message contract.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class FaultEntityNameAttribute :
    Attribute
{
    /// <summary>Initializes the attribute with the specified fault entity name.</summary>
    /// <param name="entityName">The non-empty entity name to use for faults of the message contract.</param>
    public FaultEntityNameAttribute(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        EntityName = entityName;
    }

    /// <summary>Gets the declared fault entity name.</summary>
    public string EntityName { get; }
}
