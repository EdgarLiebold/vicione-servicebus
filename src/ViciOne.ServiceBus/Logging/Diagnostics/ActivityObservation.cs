using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Isolates message processing from exceptions raised by application-owned activity listeners.</summary>
internal static class ActivityObservation
{
    public static Activity? TryCreate(Lazy<ActivitySource> source, string name, ActivityKind kind,
        System.Diagnostics.ActivityContext parentContext = default,
        IEnumerable<ActivityLink>? links = null)
    {
        try
        {
            return source.Value.CreateActivity(name, kind, parentContext, links: links);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while creating an activity");
            return null;
        }
    }

    public static bool TryStart(Activity activity)
    {
        Activity? previousActivity = Activity.Current;

        try
        {
            activity.Start();
            return true;
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while starting an activity");
            TryDispose(activity);
            Activity.Current = previousActivity;
            return false;
        }
    }

    public static void TrySetTraceState(Activity activity, string? traceState)
    {
        try
        {
            activity.TraceStateString = traceState;
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while propagating trace state");
        }
    }

    public static void TrySetTag(Activity activity, string key, object? value)
    {
        try
        {
            activity.SetTag(key, value);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while recording a tag");
        }
    }

    public static void TrySetBaggage(Activity activity, string key, string? value)
    {
        try
        {
            activity.SetBaggage(key, value);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while recording baggage");
        }
    }

    public static void TryAddEvent(Activity activity, ActivityEvent activityEvent)
    {
        try
        {
            activity.AddEvent(activityEvent);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while recording an event");
        }
    }

    public static void TrySetStatus(Activity activity, ActivityStatusCode status, string? description = null)
    {
        try
        {
            activity.SetStatus(status, description);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while recording status");
        }
    }

    public static void TryDispose(Activity activity)
    {
        try
        {
            activity.Dispose();
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while stopping an activity");
        }
    }

    static void TryLog(Exception exception, string message)
    {
        try
        {
            LogContext.Warning?.Log(exception, message);
        }
        catch
        {
            // A secondary logging failure cannot replace the messaging operation's outcome.
        }
    }
}
