using System;
using System.Diagnostics;
using System.Threading;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Owns one started activity and completes it without exposing telemetry plumbing.</summary>
internal sealed class StartedActivity : IDisposable
{
    readonly TimeProvider _timeProvider;
    int _stopped;

    public StartedActivity(Activity activity, TimeProvider? timeProvider = null)
    {
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    internal Activity Activity { get; }

    public void SetTag(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            return;

        ActivityObservation.TrySetTag(Activity, key, value);
    }

    public void Update<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.BodyLength.HasValue)
            SetTag(ServiceBusTelemetry.Attributes.MessageBodySize, context.BodyLength.Value);
    }

    public void AddExceptionEvent(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        try
        {
            exception = exception.GetBaseException() ?? exception;
        }
        catch
        {
            // Exception diagnostics cannot replace the operation failure.
        }

        var exceptionMessage = ExceptionUtil.GetMessage(exception);

        var tags = new ActivityTagsCollection
        {
            { ServiceBusTelemetry.Attributes.ExceptionMessage, exceptionMessage },
            { ServiceBusTelemetry.Attributes.ExceptionType, TypeCache.GetShortName(exception.GetType()) },
            { ServiceBusTelemetry.Attributes.ExceptionStackTrace, ExceptionUtil.GetStackTrace(exception) }
        };

        var activityEvent = new ActivityEvent(ServiceBusTelemetry.Events.Exception, _timeProvider.GetUtcNow(), tags);

        ActivityObservation.TryAddEvent(Activity, activityEvent);
        ActivityObservation.TrySetStatus(Activity, ActivityStatusCode.Error, exceptionMessage);
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        if (Activity.Status == ActivityStatusCode.Unset)
            ActivityObservation.TrySetStatus(Activity, ActivityStatusCode.Ok);

        ActivityObservation.TryDispose(Activity);
    }

    public void Dispose() => Stop();
}
