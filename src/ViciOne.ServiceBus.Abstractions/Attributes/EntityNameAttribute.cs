using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Specify the EntityName used for this message contract
/// if configured.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class EntityNameAttribute :
    Attribute
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="entityName">The entity name to use for the message type</param>
    public EntityNameAttribute(string entityName)
    {
        EntityName = entityName;
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName { get; }
}
