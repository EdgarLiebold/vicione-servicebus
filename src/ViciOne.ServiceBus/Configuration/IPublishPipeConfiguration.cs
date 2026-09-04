using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface IPublishPipeConfiguration
{
    IPublishPipeSpecification Specification { get; }
    IPublishPipeConfigurator Configurator { get; }

    IPublishPipe CreatePipe();
}
