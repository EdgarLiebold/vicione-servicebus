namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Validates entity name values.</summary>
public interface IEntityNameValidator
{
    /// <summary>Determines whether valid entity name.</summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsValidEntityName(string name);

    /// <summary>Throws when invalid entity name.</summary>
    /// <param name="name">The name.</param>
    void ThrowIfInvalidEntityName(string name);
}
