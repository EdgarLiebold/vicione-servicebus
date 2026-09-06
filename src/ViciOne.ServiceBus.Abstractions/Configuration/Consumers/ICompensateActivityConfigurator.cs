using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configure the execution of the activity and arguments with some tasty middleware.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityConfigurator<TActivity, TLog> :
    ICompensateActivityPipeConfigurator<TActivity, TLog>,
    IActivityObserverConnector,
    IConsumeConfigurator
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Gets or sets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Writes the current diagnostic event.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    new void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>Configure the arguments separate from the activity.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void ActivityLog(Action<ICompensateActivityLogConfigurator<TLog>> configure);

    /// <summary>Configure the routing slip pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void RoutingSlip(Action<IRoutingSlipConfigurator> configure);
}
