namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds middleware to the activity-bound compensation context after log deserialization.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityLogConfigurator<TActivity, TLog> :
    ICompensateActivityLogConfigurator<TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> _configurator;

    /// <summary>Wraps the activity-bound compensation pipeline that receives added specifications.</summary>
    /// <param name="configurator">The compensation-activity pipeline configurator to update.</param>
    public CompensateActivityLogConfigurator(IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _configurator = configurator;
    }

    /// <summary>Adds middleware expressed for the log-level compensation context.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TLog>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
