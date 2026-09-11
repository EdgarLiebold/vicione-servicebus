using System;
using System.Diagnostics;
using System.Threading;

namespace ViciOne.ServiceBus.Monitoring.Telemetry;

/// <summary>Owns one optional activity without allowing telemetry failures to change messaging outcomes.</summary>
internal sealed class SafeActivityScope : IDisposable
{
    Activity? _activity;

    private SafeActivityScope()
    {
    }

    public static SafeActivityScope None { get; } = new();

    public SafeActivityScope(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public void SetFailure(string errorType)
    {
        Activity? activity = Volatile.Read(ref _activity);
        if (activity is null)
            return;

        try
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag(ServiceBusTelemetry.Attributes.ErrorType, errorType);
        }
        catch
        {
            // Activity listeners cannot change the operation's failure semantics.
        }
    }

    public void SetTag(string name, object? value)
    {
        Activity? activity = Volatile.Read(ref _activity);
        if (activity is null)
            return;

        try
        {
            activity.SetTag(name, value);
        }
        catch
        {
            // Activity listeners cannot change the operation being observed.
        }
    }

    public void Dispose()
    {
        Activity? activity = Interlocked.Exchange(ref _activity, null);
        if (activity is null)
            return;

        try
        {
            activity.Dispose();
        }
        catch
        {
            // Activity disposal cannot change the operation being observed.
        }
    }
}
