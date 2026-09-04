namespace ViciOne.ServiceBus.Configuration;

public interface IConsumePipeConfiguration
{
    IConsumePipeSpecification Specification { get; }
    IConsumePipeConfigurator Configurator { get; }
}
