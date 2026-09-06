using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines publish pipe configuration.</summary>
public interface IPublishPipeConfiguration
{
    /// <summary>Gets the specification.</summary>
    IPublishPipeSpecification Specification { get; }
    /// <summary>Gets the configurator.</summary>
    IPublishPipeConfigurator Configurator { get; }

    /// <summary>Creates pipe.</summary>
    /// <returns>The created pipe.</returns>
    IPublishPipe CreatePipe();
}
