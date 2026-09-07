using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures compensation execution, activity-log binding, and routing-slip middleware.</summary>
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

    /// <summary>Configures middleware for the compensation log contract.</summary>
    /// <param name="configure">The callback that configures log middleware.</param>
    new void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>Configures middleware between log deserialization and activity invocation.</summary>
    /// <param name="configure">The callback that configures activity-log middleware.</param>
    void ActivityLog(Action<ICompensateActivityLogConfigurator<TLog>> configure);

    /// <summary>Configures middleware for the routing-slip message.</summary>
    /// <param name="configure">The callback that configures routing-slip middleware.</param>
    void RoutingSlip(Action<IRoutingSlipConfigurator> configure);
}
