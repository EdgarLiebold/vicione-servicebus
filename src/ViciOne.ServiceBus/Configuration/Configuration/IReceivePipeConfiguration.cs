// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Transports;


    public interface IReceivePipeConfiguration
    {
        ISpecification Specification { get; }
        IReceivePipeConfigurator Configurator { get; }
        IBuildPipeConfigurator<ReceiveContext> DeadLetterConfigurator { get; }
        IBuildPipeConfigurator<ExceptionReceiveContext> ErrorConfigurator { get; }

        IReceivePipe CreatePipe(IConsumePipe consumePipe, ISerialization serializers);
    }
}
