using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configure the execution of the activity and arguments with some tasty middleware.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityConfigurator<TActivity, TArguments> :
    IExecuteActivityPipeConfigurator<TActivity, TArguments>,
    IActivityObserverConnector,
    IConsumeConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Gets or sets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Configure the pipeline prior to the activity factory.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    new void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configure the arguments separate from the activity.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure);

    /// <summary>Configure the routing slip pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void RoutingSlip(Action<IRoutingSlipConfigurator> configure);
}
