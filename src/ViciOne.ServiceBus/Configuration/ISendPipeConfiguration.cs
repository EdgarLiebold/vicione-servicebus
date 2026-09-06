using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines send pipe configuration.</summary>
public interface ISendPipeConfiguration
{
    /// <summary>Gets the specification.</summary>
    ISendPipeSpecification Specification { get; }
    /// <summary>Gets the configurator.</summary>
    ISendPipeConfigurator Configurator { get; }

    /// <summary>Creates pipe.</summary>
    /// <returns>The created pipe.</returns>
    ISendPipe CreatePipe();
}
