using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish pipe configuration.
/// </summary>
public interface IPublishPipeConfiguration
{
    /// <summary>
    /// Gets the specification value.
    /// </summary>
    IPublishPipeSpecification Specification { get; }
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    IPublishPipeConfigurator Configurator { get; }

    /// <summary>
    /// Creates pipe.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IPublishPipe CreatePipe();
}
