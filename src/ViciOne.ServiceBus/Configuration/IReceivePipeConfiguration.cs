using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface IReceivePipeConfiguration
{
    ISpecification Specification { get; }
    IReceivePipeConfigurator Configurator { get; }
    IBuildPipeConfigurator<ReceiveContext> DeadLetterConfigurator { get; }
    IBuildPipeConfigurator<ExceptionReceiveContext> ErrorConfigurator { get; }

    IReceivePipe CreatePipe(IConsumePipe consumePipe, ISerialization serializers);
}
