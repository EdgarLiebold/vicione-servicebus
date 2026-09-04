using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send pipe configuration.
/// </summary>
public interface ISendPipeConfiguration
{
    /// <summary>
    /// Gets the specification value.
    /// </summary>
    ISendPipeSpecification Specification { get; }
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    ISendPipeConfigurator Configurator { get; }

    /// <summary>
    /// Creates pipe.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ISendPipe CreatePipe();
}
