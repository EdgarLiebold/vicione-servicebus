namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Validates broker entity names against transport-specific naming rules.</summary>
public interface IEntityNameValidator
{
    /// <summary>Determines whether a name satisfies the transport rules.</summary>
    /// <param name="name">The entity name to validate.</param>
    /// <returns><see langword="true" /> when the name is valid; otherwise, <see langword="false" />.</returns>
    bool IsValidEntityName(string name);

    /// <summary>Throws when a name violates the transport rules.</summary>
    /// <param name="name">The entity name to validate.</param>
    void ThrowIfInvalidEntityName(string name);
}
