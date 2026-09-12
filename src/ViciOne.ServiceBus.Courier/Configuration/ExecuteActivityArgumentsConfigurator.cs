namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds middleware to the activity-bound execution context after argument deserialization.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityArgumentsConfigurator<TActivity, TArguments> :
    IExecuteActivityArgumentsConfigurator<TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _configurator;

    /// <summary>Wraps the activity-bound execution pipeline that receives added specifications.</summary>
    /// <param name="configurator">The execution-activity pipeline configurator to update.</param>
    public ExecuteActivityArgumentsConfigurator(IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _configurator = configurator;
    }

    /// <summary>Adds middleware expressed for the argument-level execution context.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TArguments>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
