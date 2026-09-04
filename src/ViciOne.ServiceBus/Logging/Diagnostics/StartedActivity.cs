using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Represents a started activity value.
/// </summary>
public readonly struct StartedActivity
{
    /// <summary>
    /// Defines the activity value.
    /// </summary>
    public readonly Activity Activity;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public StartedActivity(Activity activity, TimeProvider? timeProvider = null)
    {
        Activity = activity;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Sets tag.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ActivityObservation.TrySetTag(Activity, key, value);
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    public void Update<T>(SendContext<T> context)
        where T : class
    {
        if (context.BodyLength.HasValue)
            SetTag(DiagnosticHeaders.Messaging.BodyLength, context.BodyLength.Value.ToString());
    }

    /// <summary>
    /// Adds exception event to the configuration.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="escaped">The escaped value.</param>
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

    /// <summary>
    /// Stops the configured component.
    /// </summary>
    public void Stop()
    {
        if (Activity.Status == ActivityStatusCode.Unset)
            ActivityObservation.TrySetStatus(Activity, ActivityStatusCode.Ok);

        ActivityObservation.TryDispose(Activity);
    }
}
