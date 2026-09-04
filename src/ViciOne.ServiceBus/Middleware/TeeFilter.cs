using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Connects multiple output pipes to a single input pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class TeeFilter<TContext> :
    ITeeFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Connectable<IPipe<TContext>> _connections;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TeeFilter()
    {
        _connections = new Connectable<IPipe<TContext>>();
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _connections.Count;

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _connections.ForEach(pipe => pipe.Probe(context));
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        var connectionsTask = _connections.ForEachAsync(pipe => pipe.SendAsync(context));
        if (connectionsTask.Status == TaskStatus.RanToCompletion)
            return next.SendAsync(context);

        async Task SendAsync()
        {
            await connectionsTask.ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>
    /// Connects pipe.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPipe(IPipe<TContext> pipe)
    {
        return _connections.Connect(pipe);
    }
}


/// <summary>
/// Connects multiple output pipes to a single input pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey">The key type</typeparam>
public class TeeFilter<TContext, TKey> :
    TeeFilter<TContext>,
    ITeeFilter<TContext, TKey>
    where TContext : class, PipeContext
    where TKey : notnull
{
    readonly KeyAccessor<TContext, TKey> _keyAccessor;
    readonly Lazy<IKeyPipeConnector<TKey>> _keyConnections;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keyAccessor">The key accessor value.</param>
    public TeeFilter(KeyAccessor<TContext, TKey> keyAccessor)
    {
        _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));

        _keyConnections = new Lazy<IKeyPipeConnector<TKey>>(ConnectKeyFilter);
    }

    /// <summary>
    /// Connects pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _keyConnections.Value.ConnectPipe(key, pipe);
    }

    IKeyPipeConnector<TKey> ConnectKeyFilter()
    {
        var filter = new KeyFilter<TContext, TKey>(_keyAccessor);

        ConnectPipe(filter.ToPipe());

        return filter;
    }
}
