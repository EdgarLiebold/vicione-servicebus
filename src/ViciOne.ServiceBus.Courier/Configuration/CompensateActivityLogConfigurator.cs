namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures compensate activity log.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityLogConfigurator<TActivity, TLog> :
    ICompensateActivityLogConfigurator<TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public CompensateActivityLogConfigurator(IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _configurator = configurator;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TLog>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
