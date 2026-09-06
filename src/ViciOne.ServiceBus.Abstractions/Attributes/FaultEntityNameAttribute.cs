using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Specify the EntityName used for the Fault version of this message contract, overriding the configured <see cref="IEntityNameFormatter" />
/// if configured.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class FaultEntityNameAttribute :
    Attribute
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityName">The entity name to use for the faulted message type.</param>
    public FaultEntityNameAttribute(string entityName)
    {
        EntityName = entityName;
    }

    /// <summary>Gets the entity name.</summary>
    public string EntityName { get; }
}
