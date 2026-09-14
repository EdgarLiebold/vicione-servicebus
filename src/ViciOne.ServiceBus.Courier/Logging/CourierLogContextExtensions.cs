using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Starts Courier metric scopes through the meter bound to a logging context.</summary>
internal static class CourierLogContextExtensions
{
    /// <summary>Starts metrics collection for an activity execution.</summary>
    /// <typeparam name="TActivity">The executing activity type.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="logContext">The logging context bound to the active meter.</param>
    /// <param name="context">The routing-slip delivery measured by the operation.</param>
    /// <returns>The execution metric operation, or <see langword="null"/> when metrics are disabled.</returns>
    public static MetricOperation? TryStartExecutionMetrics<TActivity, TArguments>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        LogContextMetricsExtensions.StartProcess(logContext, context, "execute", "courier_execute");

    /// <summary>Starts metrics collection for an activity compensation.</summary>
    /// <typeparam name="TActivity">The compensating activity type.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="logContext">The logging context bound to the active meter.</param>
    /// <param name="context">The routing-slip delivery measured by the operation.</param>
    /// <returns>The compensation metric operation, or <see langword="null"/> when metrics are disabled.</returns>
    public static MetricOperation? TryStartCompensationMetrics<TActivity, TLog>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class =>
        LogContextMetricsExtensions.StartProcess(logContext, context, "compensate", "courier_compensate");
}
