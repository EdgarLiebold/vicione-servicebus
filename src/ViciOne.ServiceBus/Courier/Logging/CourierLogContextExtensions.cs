using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides Courier-specific tracing and metrics operations.
/// </summary>
public static class CourierLogContextExtensions
{
    /// <summary>
    /// Starts tracing for an activity execution.
    /// </summary>
    public static StartedActivity? StartExecuteActivity<TActivity, TArguments>(this ILogContext logContext,
        ConsumeContext<RoutingSlip> context)
        where TActivity : IExecuteActivity<TArguments>
        where TArguments : class
    {
        return LogContextActivityExtensions.StartActivity(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.TrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TArguments>.DiagnosticAddress);
        });
    }

    /// <summary>
    /// Starts tracing for an activity compensation.
    /// </summary>
    public static StartedActivity? StartCompensateActivity<TActivity, TLog>(this ILogContext logContext,
        ConsumeContext<RoutingSlip> context)
        where TActivity : ICompensateActivity<TLog>
        where TLog : class
    {
        return LogContextActivityExtensions.StartActivity(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.TrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TLog>.DiagnosticAddress);
        });
    }

    /// <summary>
    /// Starts metrics collection for an activity execution.
    /// </summary>
    public static MetricOperation? StartActivityExecuteInstrument<TActivity, TArguments>(this ILogContext logContext,
        ConsumeContext<RoutingSlip> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "execute", "courier_execute");

    /// <summary>
    /// Starts metrics collection for an activity compensation.
    /// </summary>
    public static MetricOperation? StartActivityCompensateInstrument<TActivity, TLog>(this ILogContext logContext,
        ConsumeContext<RoutingSlip> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "compensate", "courier_compensate");
}
