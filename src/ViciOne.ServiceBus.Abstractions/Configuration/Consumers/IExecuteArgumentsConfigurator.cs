namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the middleware pipeline that processes an activity's execution arguments.</summary>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
public interface IExecuteArgumentsConfigurator<TArguments> :
    IPipeConfigurator<ExecuteContext<TArguments>>,
    IConsumeConfigurator
    where TArguments : class
{
}
