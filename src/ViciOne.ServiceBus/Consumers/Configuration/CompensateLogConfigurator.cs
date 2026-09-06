namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures compensate log.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateLogConfigurator<TLog> :
    ICompensateLogConfigurator<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateContext<TLog>> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public CompensateLogConfigurator(IPipeConfigurator<CompensateContext<TLog>> configurator)
    {
        _configurator = configurator;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateContext<TLog>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }
}
