namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures execute activity arguments.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityArgumentsConfigurator<TActivity, TArguments> :
    IExecuteActivityArgumentsConfigurator<TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public ExecuteActivityArgumentsConfigurator(IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TArguments>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
