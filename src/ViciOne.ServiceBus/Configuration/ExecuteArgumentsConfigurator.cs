using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds middleware to an activity-execution argument pipeline.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public sealed class ExecuteArgumentsConfigurator<TArguments> :
    IExecuteArgumentsConfigurator<TArguments>
    where TArguments : class
{
    readonly IPipeConfigurator<ExecuteContext<TArguments>> _configurator;

    /// <summary>Creates an adapter that adds middleware to an activity-execution pipe.</summary>
    /// <param name="configurator">The underlying execution-context pipe configuration.</param>
    public ExecuteArgumentsConfigurator(IPipeConfigurator<ExecuteContext<TArguments>> configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    /// <summary>Adds middleware to the activity-execution pipe.</summary>
    /// <param name="specification">The execution-context middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteContext<TArguments>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
