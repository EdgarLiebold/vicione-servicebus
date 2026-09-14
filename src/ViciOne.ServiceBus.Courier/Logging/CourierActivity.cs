using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Creates process activities for Courier execution and compensation.</summary>
internal static class CourierActivity
{
    /// <summary>Starts a process activity for routing-slip activity execution.</summary>
    /// <typeparam name="TActivity">The executing activity type recorded as the processor.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract recorded as the message contract.</typeparam>
    /// <param name="context">The routing-slip delivery that supplies trace identity and tracking metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when tracing is unavailable.</returns>
    public static StartedActivity? TryStartExecution<TActivity, TArguments>(ConsumeContext<IRoutingSlip> context)
        where TActivity : IExecuteActivity<TArguments>
        where TArguments : class =>
        MessageActivity.TryStartProcess(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CourierTrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TArguments>.DiagnosticAddress);
        });

    /// <summary>Starts a process activity for routing-slip activity compensation.</summary>
    /// <typeparam name="TActivity">The compensating activity type recorded as the processor.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract recorded as the message contract.</typeparam>
    /// <param name="context">The routing-slip delivery that supplies trace identity and tracking metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when tracing is unavailable.</returns>
    public static StartedActivity? TryStartCompensation<TActivity, TLog>(ConsumeContext<IRoutingSlip> context)
        where TActivity : ICompensateActivity<TLog>
        where TLog : class =>
        MessageActivity.TryStartProcess(context.Advanced(), activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CourierTrackingNumber, context.Message.TrackingNumber.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TActivity>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TLog>.DiagnosticAddress);
        });
}
