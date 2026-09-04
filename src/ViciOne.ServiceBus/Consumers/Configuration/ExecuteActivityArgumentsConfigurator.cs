namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an execute activity arguments configurator implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityArgumentsConfigurator<TActivity, TArguments> :
    IExecuteActivityArgumentsConfigurator<TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public ExecuteActivityArgumentsConfigurator(IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TArguments>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
