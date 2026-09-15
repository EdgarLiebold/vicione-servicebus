using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches a context to connected output pipelines before invoking the continuation.</summary>
/// <typeparam name="TContext">The context contract accepted by the connected pipelines.</typeparam>
public class TeeFilter<TContext> :
    ITeeFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Connectable<IPipe<TContext>> _connections;

    /// <summary>Creates a dispatcher with no connected output pipelines.</summary>
    public TeeFilter()
    {
        _connections = new Connectable<IPipe<TContext>>();
    }

    /// <summary>Gets the number of directly connected output pipelines.</summary>
    public int Count => _connections.Count;

    /// <summary>Probes each connected output pipeline.</summary>
    /// <param name="context">The diagnostic context shared with the connected pipelines.</param>
    public void Probe(ProbeContext context)
    {
        _connections.ForEach(pipe => pipe.Probe(context));
    }

    /// <summary>Awaits connected pipeline dispatch and invokes the continuation only after successful completion.</summary>
    /// <param name="context">The context dispatched to every connected pipeline and the continuation.</param>
    /// <param name="next">The continuation invoked after all connected pipelines complete successfully.</param>
    /// <returns>The task covering connected pipeline dispatch and continuation execution.</returns>
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

    /// <summary>Registers an output pipeline for context dispatch.</summary>
    /// <param name="pipe">The output pipeline receiving dispatched contexts.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe(IPipe<TContext> pipe)
    {
        return _connections.Connect(pipe);
    }
}


/// <summary>Adds key-selected output registration to a dispatcher that also supports directly connected pipelines.</summary>
/// <typeparam name="TContext">The context contract used to select a routing key.</typeparam>
/// <typeparam name="TKey">The routing key identifying selected output pipelines.</typeparam>
public class TeeFilter<TContext, TKey> :
    TeeFilter<TContext>,
    ITeeFilter<TContext, TKey>
    where TContext : class, PipeContext
    where TKey : notnull
{
    readonly KeyAccessor<TContext, TKey> _keyAccessor;
    readonly Lazy<IKeyPipeConnector<TKey>> _keyConnections;

    /// <summary>Creates a keyed dispatcher whose key-routing pipeline is connected on first keyed registration.</summary>
    /// <param name="keyAccessor">The required accessor selecting a routing key from each dispatched context.</param>
    public TeeFilter(KeyAccessor<TContext, TKey> keyAccessor)
    {
        _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));

        _keyConnections = new Lazy<IKeyPipeConnector<TKey>>(ConnectKeyFilter);
    }

    /// <summary>Registers an output pipeline for contexts routed to the specified key.</summary>
    /// <typeparam name="T">The context contract accepted by the keyed output pipeline.</typeparam>
    /// <param name="key">The routing key selecting the output pipeline.</param>
    /// <param name="pipe">The keyed output pipeline receiving matching contexts.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
