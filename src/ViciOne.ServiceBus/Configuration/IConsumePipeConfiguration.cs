namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume pipe configuration.
/// </summary>
public interface IConsumePipeConfiguration
{
    /// <summary>
    /// Gets the specification value.
    /// </summary>
    IConsumePipeSpecification Specification { get; }
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    IConsumePipeConfigurator Configurator { get; }
}
