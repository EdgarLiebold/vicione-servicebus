namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents a boolean condition.</summary>
public interface ICondition
{
    /// <summary>Gets a value indicating whether met.</summary>
    bool IsMet { get; }
}
