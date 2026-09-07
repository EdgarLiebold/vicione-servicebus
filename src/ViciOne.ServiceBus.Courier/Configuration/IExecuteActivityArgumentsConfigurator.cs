namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware applied to deserialized activity arguments.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityArgumentsConfigurator<TArguments> :
    IPipeConfigurator<ExecuteActivityContext<TArguments>>,
    IConsumeConfigurator
    where TArguments : class
{
}
