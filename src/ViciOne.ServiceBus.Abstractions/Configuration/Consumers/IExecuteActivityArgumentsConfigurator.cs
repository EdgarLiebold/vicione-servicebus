namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configure the execution of the activity and arguments with some tasty middleware.
/// </summary>
/// <typeparam name="TArguments"></typeparam>
public interface IExecuteActivityArgumentsConfigurator<TArguments> :
    IPipeConfigurator<ExecuteActivityContext<TArguments>>,
    IConsumeConfigurator
    where TArguments : class
{
}
