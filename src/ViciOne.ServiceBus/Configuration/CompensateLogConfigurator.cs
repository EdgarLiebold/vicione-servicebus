using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds middleware to an activity-compensation log pipeline.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public sealed class CompensateLogConfigurator<TLog> :
    ICompensateLogConfigurator<TLog>
    where TLog : class
{
    readonly IPipeConfigurator<CompensateContext<TLog>> _configurator;

    /// <summary>Creates an adapter that adds middleware to a compensation-log pipe.</summary>
    /// <param name="configurator">The underlying compensation-context pipe configuration.</param>
    public CompensateLogConfigurator(IPipeConfigurator<CompensateContext<TLog>> configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    /// <summary>Adds middleware to the compensation-log pipe.</summary>
    /// <param name="specification">The compensation-context middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateContext<TLog>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
