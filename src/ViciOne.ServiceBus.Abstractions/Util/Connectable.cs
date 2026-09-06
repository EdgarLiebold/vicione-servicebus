using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Maintains a collection of connections of the generic type.</summary>
/// <typeparam name="T">The connectable type.</typeparam>
public class Connectable<T>
    where T : class
{
    readonly Dictionary<long, T> _connections;
    T[]? _connected;
    long _nextId;

    /// <summary>Initializes a new instance.</summary>
    public Connectable()
    {
        _connections = new Dictionary<long, T>();
        _connected = null;
    }

    /// <summary>
    /// Returns a point-in-time snapshot of the connected instances. Modifying the returned
    /// array does not change this connection set.
    /// </summary>
    public T[] Connected => [.. GetConnected()];

    /// <summary>The number of connections.</summary>
    public int Count => GetConnected().Length;

    /// <summary>Connect a connectable type.</summary>
    /// <param name="connection">The connection to add.</param>
    /// <returns>The connection handle.</returns>
    public ConnectHandle Connect(T connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Interlocked.Increment(ref _nextId);

        lock (_connections)
        {
            _connections.Add(id, connection);
            _connected = null;
        }

        return new Handle(id, this);
    }

    /// <summary>Enumerate the connections invoking the callback for each connection.</summary>
    /// <param name="callback">The callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An awaitable Task for the operation.</returns>
    public Task ForEachAsync(Func<T, Task> callback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); ArgumentNullException.ThrowIfNull(callback);

        T[] connected = GetConnected();

        if (connected.Length == 0)
            return Task.CompletedTask;

        if (connected.Length == 1)
            return InvokeCallbackAsync(connected[0], callback);

        var outputTasks = new Task[connected.Length];
        int i;
        for (i = 0; i < connected.Length; i++)
            outputTasks[i] = InvokeCallbackAsync(connected[i], callback);

        for (i = 0; i < outputTasks.Length; i++)
        {
            if (outputTasks[i].Status != TaskStatus.RanToCompletion)
                break;
        }

        if (i == outputTasks.Length)
            return Task.CompletedTask;

        return Task.WhenAll(outputTasks);
    }

    /// <summary>Invokes <paramref name="callback" /> for every instance in a stable point-in-time snapshot.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ForEach(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        T[] connected = GetConnected();

        switch (connected.Length)
        {
            case 0:
                break;
            case 1:
                callback(connected[0]);
                break;
            default:
                {
                    for (var i = 0; i < connected.Length; i++)
                        callback(connected[i]);
                    break;
                }
        }
    }

    /// <summary>
    /// Returns whether <paramref name="callback" /> accepts every instance in a stable
    /// point-in-time snapshot, stopping at the first rejection.
    /// </summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool All(Func<T, bool> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        T[] connected = GetConnected();

        if (connected.Length == 0)
            return true;

        if (connected.Length == 1)
            return callback(connected[0]);

        for (var i = 0; i < connected.Length; i++)
        {
            if (callback(connected[i]) == false)
                return false;
        }

        return true;
    }

    T[] GetConnected()
    {
        T[]? read = Volatile.Read(ref _connected);
        if (read != null)
            return read;

        lock (_connections)
        {
            read = Volatile.Read(ref _connected);
            if (read != null)
                return read;

            var connected = new T[_connections.Count];
            _connections.Values.CopyTo(connected, 0);

            Volatile.Write(ref _connected, connected);

            return connected;
        }
    }

    static Task InvokeCallbackAsync(T connection, Func<T, Task> callback)
    {
        try
        {
            return callback(connection)
                ?? Task.FromException(new InvalidOperationException("The connection callback returned a null task."));
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    void Disconnect(long id)
    {
        lock (_connections)
        {
            _connections.Remove(id);
            _connected = null;
        }
    }


    class Handle :
        ConnectHandle
    {
        readonly Connectable<T> _connectable;
        readonly long _id;

        public Handle(long id, Connectable<T> connectable)
        {
            _id = id;
            _connectable = connectable;
        }

        public void Disconnect()
        {
            _connectable.Disconnect(_id);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
