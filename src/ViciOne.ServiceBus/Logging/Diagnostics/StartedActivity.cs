using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Represents a started activity.</summary>
public readonly struct StartedActivity
{
    /// <summary>Exposes the activity used by the containing type.</summary>
    public readonly Activity Activity;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public StartedActivity(Activity activity, TimeProvider? timeProvider = null)
    {
        Activity = activity;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Sets tag.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public void SetTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ActivityObservation.TrySetTag(Activity, key, value);
    }

    /// <summary>Updates the current value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    public void Update<T>(SendContext<T> context)
        where T : class
    {
        if (context.BodyLength.HasValue)
            SetTag(DiagnosticHeaders.Messaging.BodyLength, context.BodyLength.Value.ToString());
    }

    /// <summary>Adds exception event to the configuration.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="escaped">The escaped.</param>
    public void AddExceptionEvent(Exception exception, bool escaped = true)
    {
        exception = exception.GetBaseException() ?? exception;

        var exceptionMessage = ExceptionUtil.GetMessage(exception);

        var tags = new ActivityTagsCollection
        {
            { DiagnosticHeaders.Exceptions.Escaped, escaped },
            { DiagnosticHeaders.Exceptions.Message, exceptionMessage },
            { DiagnosticHeaders.Exceptions.Type, TypeCache.GetShortName(exception.GetType()) },
            { DiagnosticHeaders.Exceptions.Stacktrace, ExceptionUtil.GetStackTrace(exception) }
        };

        var activityEvent = new ActivityEvent(DiagnosticHeaders.Exceptions.EventName, _timeProvider.GetUtcNow(), tags);

        ActivityObservation.TryAddEvent(Activity, activityEvent);
        ActivityObservation.TrySetStatus(Activity, ActivityStatusCode.Error, exceptionMessage);
    }

    /// <summary>Stops the configured component.</summary>
    public void Stop()
    {
        if (Activity.Status == ActivityStatusCode.Unset)
            ActivityObservation.TrySetStatus(Activity, ActivityStatusCode.Ok);

        ActivityObservation.TryDispose(Activity);
    }
}
