using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Isolates message processing from failures while creating, mutating, or completing diagnostic activities.</summary>
internal static class ActivityObservation
{
    const string PreviousActivityProperty = "ViciOne.ServiceBus.ActivityObservation.PreviousActivity";

    sealed class AmbientActivity(Activity? value)
    {
        public Activity? Value { get; } = value;
    }

    public static Activity? TryStartSource(ActivitySource source, string name, ActivityKind kind,
        System.Diagnostics.ActivityContext? parentContext = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        Activity? previousActivity = Activity.Current;
        Activity? activity;
        try
        {
            activity = parentContext is { } parent
                ? source.CreateActivity(name, kind, parent, tags)
                : source.CreateActivity(name, kind);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while creating an activity");
            activity = null;
        }
        finally
        {
            Activity.Current = previousActivity;
        }

        return activity is not null && TryStart(activity) ? activity : null;
    }

    public static Activity? TryCreate(Lazy<ActivitySource> source, string name, ActivityKind kind,
        System.Diagnostics.ActivityContext parentContext = default,
        IEnumerable<ActivityLink>? links = null,
        bool newRoot = false)
    {
        Activity? previousActivity = Activity.Current;
        try
        {
            if (newRoot)
                Activity.Current = null;
            return source.Value.CreateActivity(name, kind, parentContext, links: links);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while creating an activity");
            return null;
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    public static bool TryStart(Activity activity, bool newRoot = false)
    {
        Activity? previousActivity = Activity.Current;

        try
        {
            activity.SetCustomProperty(PreviousActivityProperty, new AmbientActivity(previousActivity));
            if (newRoot)
                Activity.Current = null;
            activity.Start();
            if (activity.IsStopped)
            {
                Activity.Current = previousActivity;
                return false;
            }
            Activity.Current = activity;
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

    public static void TrySetTag(Activity activity, string key, object? value)
    {
        try
        {
            activity.SetTag(key, value);
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity observation failed while recording a tag");
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
            TryLog(exception, "Activity observation failed while recording baggage");
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
            TryLog(exception, "Activity observation failed while recording an event");
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
            TryLog(exception, "Activity observation failed while recording status");
        }
    }

    public static void TryDispose(Activity activity)
    {
        Activity? previousActivity = Activity.Current;
        var capturedAmbient = activity.GetCustomProperty(PreviousActivityProperty) as AmbientActivity;
        Activity? expectedActivity = ReferenceEquals(previousActivity, activity)
            ? capturedAmbient is null ? activity.Parent : capturedAmbient.Value
            : previousActivity;
        TryDispose(activity, expectedActivity);
    }

    public static void TryDispose(Activity activity, Activity? expectedActivity)
    {
        try
        {
            activity.Dispose();
        }
        catch (Exception exception)
        {
            TryLog(exception, "Activity listener faulted while stopping an activity");
        }
        finally
        {
            Activity.Current = expectedActivity;
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
