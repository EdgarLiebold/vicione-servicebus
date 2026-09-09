using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Controls a set of registrations as one synchronous or asynchronous lifetime.</summary>
public class MultipleConnectHandle :
    ConnectHandle
{
    readonly ConnectHandle[] _handles;

    /// <summary>Creates a composite over the supplied registrations.</summary>
    /// <param name="handles">The registrations to disconnect together.</param>
    public MultipleConnectHandle(IEnumerable<ConnectHandle> handles)
    {
        ArgumentNullException.ThrowIfNull(handles);

        _handles = [.. handles];
        ThrowIfAnyHandleIsNull(_handles, nameof(handles));
    }

    /// <summary>Creates a composite over the supplied registrations.</summary>
    /// <param name="handles">The registrations to disconnect together.</param>
    public MultipleConnectHandle(params ConnectHandle[] handles)
    {
        ArgumentNullException.ThrowIfNull(handles);
        ThrowIfAnyHandleIsNull(handles, nameof(handles));

        _handles = [.. handles];
    }

    /// <summary>Attempts to disconnect every registration and reports any resulting failures.</summary>
    public void Disconnect()
    {
        List<Exception>? failures = null;
        for (var i = 0; i < _handles.Length; i++)
        {
            try
            {
                _handles[i].Disconnect();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException("Multiple registrations failed to disconnect.", failures);
    }

    /// <summary>Disconnects every registration synchronously.</summary>
    public void Dispose()
    {
        Disconnect();
    }

    /// <summary>Disconnects every registration and awaits cleanup owned by its individual handle.</summary>
    /// <returns>A value task that completes after every registration has released its owned resources.</returns>
    public async ValueTask DisposeAsync()
    {
        var cleanupTasks = new Task[_handles.Length];
        for (var i = 0; i < _handles.Length; i++)
        {
            try
            {
                cleanupTasks[i] = ((IAsyncDisposable)_handles[i]).DisposeAsync().AsTask();
            }
            catch (Exception exception)
            {
                cleanupTasks[i] = Task.FromException(exception);
            }
        }

        await Task.WhenAll(cleanupTasks).ConfigureAwait(false);
    }

    static void ThrowIfAnyHandleIsNull(ConnectHandle[] handles, string parameterName)
    {
        for (var i = 0; i < handles.Length; i++)
        {
            if (handles[i] == null)
                throw new ArgumentException("The collection cannot contain a null registration.", parameterName);
        }
    }
}
