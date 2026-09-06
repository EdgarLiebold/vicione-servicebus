namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines consume pipe configuration.</summary>
public interface IConsumePipeConfiguration
{
    /// <summary>Gets the specification.</summary>
    IConsumePipeSpecification Specification { get; }
    /// <summary>Gets the configurator.</summary>
    IConsumePipeConfigurator Configurator { get; }
}
