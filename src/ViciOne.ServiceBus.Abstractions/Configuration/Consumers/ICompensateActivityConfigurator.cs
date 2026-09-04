using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configure the execution of the activity and arguments with some tasty middleware.
/// </summary>
/// <typeparam name="TActivity"></typeparam>
/// <typeparam name="TLog"></typeparam>
public interface ICompensateActivityConfigurator<TActivity, TLog> :
    IPipeConfigurator<CompensateActivityContext<TActivity, TLog>>,
    IActivityObserverConnector,
    IConsumeConfigurator
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>
    /// Performs the log operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void Log(Action<ICompensateLogConfigurator<TLog>> configure);

    /// <summary>
    /// Configure the arguments separate from the activity
    /// </summary>
    void ActivityLog(Action<ICompensateActivityLogConfigurator<TLog>> configure);

    /// <summary>
    /// Configure the routing slip pipe
    /// </summary>
    void RoutingSlip(Action<IRoutingSlipConfigurator> configure);
}
