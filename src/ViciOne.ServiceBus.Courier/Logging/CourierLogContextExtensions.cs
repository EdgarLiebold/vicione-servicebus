using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Provides Courier-specific tracing and metrics operations.</summary>
internal static class CourierLogContextExtensions
{
    /// <summary>Starts tracing for an activity execution.</summary>
    /// <typeparam name="TActivity">The executing activity type recorded as the processor.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract recorded as the message contract.</typeparam>
    /// <param name="logContext">The active logging context that enables this extension.</param>
    /// <param name="context">The routing-slip delivery that supplies trace identity and tracking metadata.</param>
    /// <returns>The started trace activity, or <see langword="null"/> when tracing is disabled.</returns>
    public static StartedActivity? StartExecuteActivity<TActivity, TArguments>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : IExecuteActivity<TArguments>
        where TArguments : class
    {
        return LogContextActivityExtensions.StartActivity(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CourierTrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TArguments>.DiagnosticAddress);
        });
    }

    /// <summary>Starts tracing for an activity compensation.</summary>
    /// <typeparam name="TActivity">The compensating activity type recorded as the processor.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract recorded as the message contract.</typeparam>
    /// <param name="logContext">The active logging context that enables this extension.</param>
    /// <param name="context">The routing-slip delivery that supplies trace identity and tracking metadata.</param>
    /// <returns>The started trace activity, or <see langword="null"/> when tracing is disabled.</returns>
    public static StartedActivity? StartCompensateActivity<TActivity, TLog>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : ICompensateActivity<TLog>
        where TLog : class
    {
        return LogContextActivityExtensions.StartActivity(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CourierTrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TLog>.DiagnosticAddress);
        });
    }

    /// <summary>Starts metrics collection for an activity execution.</summary>
    /// <typeparam name="TActivity">The executing activity type.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="logContext">The active logging context used to start instrumentation.</param>
    /// <param name="context">The routing-slip delivery measured by the operation.</param>
    /// <returns>The execution metric operation, or <see langword="null"/> when metrics are disabled.</returns>
    public static MetricOperation? StartActivityExecuteInstrument<TActivity, TArguments>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "execute", "courier_execute");

    /// <summary>Starts metrics collection for an activity compensation.</summary>
    /// <typeparam name="TActivity">The compensating activity type.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="logContext">The active logging context used to start instrumentation.</param>
    /// <param name="context">The routing-slip delivery measured by the operation.</param>
    /// <returns>The compensation metric operation, or <see langword="null"/> when metrics are disabled.</returns>
    public static MetricOperation? StartActivityCompensateInstrument<TActivity, TLog>(this ILogContext logContext,
        ConsumeContext<IRoutingSlip> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "compensate", "courier_compensate");
}
