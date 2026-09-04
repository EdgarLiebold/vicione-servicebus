namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for definition.
/// </summary>
public interface IDefinition
{
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { get; }
}
