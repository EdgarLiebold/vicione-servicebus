using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures activity execution, argument binding, and routing-slip middleware.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityConfigurator<TActivity, TArguments> :
    IExecuteActivityPipeConfigurator<TActivity, TArguments>,
    IActivityObserverConnector,
    IConsumeConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Sets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Configures middleware before the activity instance is resolved.</summary>
    /// <param name="configure">The callback that configures argument middleware.</param>
    new void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configures middleware between argument deserialization and activity invocation.</summary>
    /// <param name="configure">The callback that configures activity-argument middleware.</param>
    void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configures middleware for the routing-slip message.</summary>
    /// <param name="configure">The callback that configures routing-slip middleware.</param>
    void RoutingSlip(Action<IRoutingSlipConfigurator> configure);
}
