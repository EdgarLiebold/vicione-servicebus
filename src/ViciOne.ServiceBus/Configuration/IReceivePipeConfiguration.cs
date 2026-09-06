using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines receive pipe configuration.</summary>
public interface IReceivePipeConfiguration
{
    /// <summary>Gets the specification.</summary>
    ISpecification Specification { get; }
    /// <summary>Gets the configurator.</summary>
    IReceivePipeConfigurator Configurator { get; }
    /// <summary>Gets the dead letter configurator.</summary>
    IBuildPipeConfigurator<ReceiveContext> DeadLetterConfigurator { get; }
    /// <summary>Gets the error configurator.</summary>
    IBuildPipeConfigurator<ExceptionReceiveContext> ErrorConfigurator { get; }

    /// <summary>Creates pipe.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="serializers">The serializers.</param>
    /// <returns>The created pipe.</returns>
    IReceivePipe CreatePipe(IConsumePipe consumePipe, ISerialization serializers);
}
