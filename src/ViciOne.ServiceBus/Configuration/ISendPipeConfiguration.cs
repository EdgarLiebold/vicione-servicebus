using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface ISendPipeConfiguration
{
    ISendPipeSpecification Specification { get; }
    ISendPipeConfigurator Configurator { get; }

    ISendPipe CreatePipe();
}
