namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Defines the contract for entity name validator.
/// </summary>
public interface IEntityNameValidator
{
    /// <summary>
    /// Determines whether valid entity name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsValidEntityName(string name);

    /// <summary>
    /// Performs the throw if invalid entity name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    void ThrowIfInvalidEntityName(string name);
}
