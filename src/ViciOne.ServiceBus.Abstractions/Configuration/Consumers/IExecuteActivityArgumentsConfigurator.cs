// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
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
}
