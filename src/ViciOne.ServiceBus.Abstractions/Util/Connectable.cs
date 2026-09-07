using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Maintains thread-safe registrations and dispatches callbacks over stable point-in-time snapshots.</summary>
/// <typeparam name="T">The registered connection type.</typeparam>
public class Connectable<T>
    where T : class
{
    readonly Dictionary<long, T> _connections;
    T[]? _connected;
    long _nextId;

    /// <summary>Initializes an empty connection set.</summary>
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

    /// <summary>Gets the number of currently registered connections.</summary>
    public int Count => GetConnected().Length;

    /// <summary>Adds one connection.</summary>
    /// <param name="connection">The connection to register.</param>
    /// <returns>An idempotent handle that removes this registration.</returns>
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

    /// <summary>Invokes an asynchronous callback for every connection in a stable point-in-time snapshot.</summary>
    /// <param name="callback">The callback to invoke for each connection.</param>
    /// <param name="cancellationToken">The token that cancels dispatch before callbacks begin.</param>
    /// <returns>A task that completes when every invoked callback has completed.</returns>
    public Task ForEachAsync(Func<T, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

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
    /// <param name="callback">The callback to invoke for each connection.</param>
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
    /// <param name="callback">The predicate to evaluate for each connection.</param>
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
    sealed class Handle :
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
