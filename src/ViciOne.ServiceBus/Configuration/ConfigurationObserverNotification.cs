using System;
using System.Runtime.ExceptionServices;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Coordinates final configuration-observer notification. Notification is attempted exactly once.
/// A failure permanently faults the mutable configuration object because observers may already
/// have applied side effects that cannot be rolled back safely. Re-entry is rejected instead of
/// exposing a partially configured pipeline.
/// </summary>
internal sealed class ConfigurationObserverNotification
{
    readonly object _lock = new object();
    ExceptionDispatchInfo _failure = null!;
    NotificationState _state;

    public void EnsureNotified(Action notify)
    {
        ArgumentNullException.ThrowIfNull(notify);

        lock (_lock)
        {
            if (_state == NotificationState.Completed)
                return;

            if (_state == NotificationState.Faulted)
            {
                _failure.Throw();
                return;
            }

            if (_state == NotificationState.Notifying)
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Configuration Observer Notification", "unknown", "A configuration observer re-entered notification for the same configuration object.", "Correct the named configuration before starting the host"));
            }

            _state = NotificationState.Notifying;
            try
            {
                notify();
                _state = NotificationState.Completed;
            }
            catch (Exception exception)
            {
                _failure = ExceptionDispatchInfo.Capture(exception);
                _state = NotificationState.Faulted;
                throw;
            }
        }
    }


    enum NotificationState
    {
        NotStarted,
        Notifying,
        Completed,
        Faulted,
    }
}
