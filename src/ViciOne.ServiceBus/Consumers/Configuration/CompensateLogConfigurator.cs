namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a compensate log configurator implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateLogConfigurator<TLog> :
    ICompensateLogConfigurator<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateContext<TLog>> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public CompensateLogConfigurator(IPipeConfigurator<CompensateContext<TLog>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateContext<TLog>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
