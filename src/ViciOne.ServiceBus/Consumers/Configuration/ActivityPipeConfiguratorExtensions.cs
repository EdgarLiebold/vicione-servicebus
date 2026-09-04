using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for activity pipe configurator.
/// </summary>
public static class ActivityPipeConfiguratorExtensions
{
    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="specification">The specification value.</param>
    public static void AddPipeSpecification<TActivity, TArguments>(this IPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> configurator,
        IPipeSpecification<ExecuteActivityContext<TArguments>> specification)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        IPipeSpecification<ExecuteActivityContext<TActivity, TArguments>> filterSpecification =
            new PipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>.SplitFilterPipeSpecification<ExecuteActivityContext<TArguments>>(
                specification, (input, context) => input, context => context);

        configurator.AddPipeSpecification(filterSpecification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="specification">The specification value.</param>
    public static void AddPipeSpecification<TActivity, TLog>(this IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator,
        IPipeSpecification<CompensateActivityContext<TLog>> specification)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        IPipeSpecification<CompensateActivityContext<TActivity, TLog>> filterSpecification =
            new PipeConfigurator<CompensateActivityContext<TActivity, TLog>>.SplitFilterPipeSpecification<CompensateActivityContext<TLog>>(specification,
                (input, context) => input, context => context);

        configurator.AddPipeSpecification(filterSpecification);
    }
}
