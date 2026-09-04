namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Represents a boolean condition
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Gets the is met value.
    /// </summary>
    bool IsMet { get; }
}
