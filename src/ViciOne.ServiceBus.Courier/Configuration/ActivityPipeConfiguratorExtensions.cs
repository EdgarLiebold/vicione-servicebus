using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds middleware to activity-bound execution and compensation pipelines.</summary>
public static class ActivityPipeConfiguratorExtensions
{
    /// <summary>Adds middleware to the resolved-activity execution pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The activity execution pipeline to update.</param>
    /// <param name="specification">The pipeline specification to add.</param>
    public static void AddPipeSpecification<TActivity, TArguments>(this IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> configurator,
        IPipeSpecification<ExecuteActivityContext<TArguments>> specification)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(specification);

        IPipeSpecification<ExecuteActivityContext<TActivity, TArguments>> filterSpecification =
            new PipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>.SplitFilterPipeSpecification<ExecuteActivityContext<TArguments>>(
                specification, (input, context) => input, context => context);

        configurator.AddPipeSpecification(filterSpecification);
    }

    /// <summary>Adds middleware to the resolved-activity compensation pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The activity compensation pipeline to update.</param>
    /// <param name="specification">The pipeline specification to add.</param>
    public static void AddPipeSpecification<TActivity, TLog>(this IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator,
        IPipeSpecification<CompensateActivityContext<TLog>> specification)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(specification);

        IPipeSpecification<CompensateActivityContext<TActivity, TLog>> filterSpecification =
            new PipeConfigurator<CompensateActivityContext<TActivity, TLog>>.SplitFilterPipeSpecification<CompensateActivityContext<TLog>>(specification,
                (input, context) => input, context => context);

        configurator.AddPipeSpecification(filterSpecification);
    }
}
