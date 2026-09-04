using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for receive pipe configuration.
/// </summary>
public interface IReceivePipeConfiguration
{
    /// <summary>
    /// Gets the specification value.
    /// </summary>
    ISpecification Specification { get; }
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    IReceivePipeConfigurator Configurator { get; }
    /// <summary>
    /// Gets the dead letter configurator value.
    /// </summary>
    IBuildPipeConfigurator<ReceiveContext> DeadLetterConfigurator { get; }
    /// <summary>
    /// Gets the error configurator value.
    /// </summary>
    IBuildPipeConfigurator<ExceptionReceiveContext> ErrorConfigurator { get; }

    /// <summary>
    /// Creates pipe.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="serializers">The serializers value.</param>
    /// <returns>The result of the operation.</returns>
    IReceivePipe CreatePipe(IConsumePipe consumePipe, ISerialization serializers);
}
