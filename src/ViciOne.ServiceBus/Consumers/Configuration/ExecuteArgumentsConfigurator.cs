namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an execute arguments configurator implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteArgumentsConfigurator<TArguments> :
    IExecuteArgumentsConfigurator<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteContext<TArguments>> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public ExecuteArgumentsConfigurator(IPipeConfigurator<ExecuteContext<TArguments>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteContext<TArguments>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
