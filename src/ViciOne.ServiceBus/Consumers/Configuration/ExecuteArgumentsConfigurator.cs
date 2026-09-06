namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures execute arguments.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteArgumentsConfigurator<TArguments> :
    IExecuteArgumentsConfigurator<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteContext<TArguments>> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public ExecuteArgumentsConfigurator(IPipeConfigurator<ExecuteContext<TArguments>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteContext<TArguments>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
