using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.Monitoring.Telemetry;

/// <summary>Owns an optional activity without allowing telemetry failures to change messaging outcomes.</summary>
internal readonly struct SafeActivityScope : IDisposable
{
    readonly Activity? _activity;

    public SafeActivityScope(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public void SetFailure(string errorType)
    {
        if (_activity is null)
            return;

        try
        {
            _activity.SetStatus(ActivityStatusCode.Error);
            _activity.SetTag(ServiceBusTelemetry.Attributes.ErrorType, errorType);
        }
        catch
        {
            // Activity listeners cannot change the operation's failure semantics.
        }
    }

    public void SetTag(string name, object? value)
    {
        if (_activity is null)
            return;

        try
        {
            _activity.SetTag(name, value);
        }
        catch
        {
            // Activity listeners cannot change the operation being observed.
        }
    }

    public void Dispose()
    {
        if (_activity is null)
            return;

        try
        {
            _activity.Dispose();
        }
        catch
        {
            // Activity disposal cannot change the operation being observed.
        }
    }
}
