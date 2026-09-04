namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a compensate activity log configurator implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateActivityLogConfigurator<TActivity, TLog> :
    ICompensateActivityLogConfigurator<TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public CompensateActivityLogConfigurator(IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TLog>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
