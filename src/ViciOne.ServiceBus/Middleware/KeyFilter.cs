using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Handles the registration of requests and connecting them to the consume pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public class KeyFilter<TContext, TKey> :
    IFilter<TContext>,
    IKeyPipeConnector<TKey>
    where TContext : class, PipeContext
    where TKey : notnull
{
    readonly KeyAccessor<TContext, TKey> _keyAccessor;
    readonly ConcurrentDictionary<TKey, IPipe<TContext>> _pipes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="keyAccessor">The key accessor.</param>
    public KeyFilter(KeyAccessor<TContext, TKey> keyAccessor)
    {
        _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));
        _pipes = new ConcurrentDictionary<TKey, IPipe<TContext>>();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("key");

        ICollection<IPipe<TContext>> pipes = _pipes.Values;
        scope.Add("count", pipes.Count);

        foreach (IPipe<TContext> pipe in pipes)
            pipe.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        var key = _keyAccessor(context);
        if (key == null)
            throw new InvalidOperationException("The key accessor returned null.");

        if (_pipes.TryGetValue(key, out IPipe<TContext>? pipe))
            await pipe.SendAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(key);

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        if (pipe is IPipe<TContext> keyPipe)
        {
            var added = _pipes.TryAdd(key, keyPipe);
            if (!added)
                throw new DuplicateKeyPipeConfigurationException($"A pipe with the specified key already exists: {key}");

            return new Handle(key, RemovePipe);
        }

        throw new ArgumentException($"The pipe must match the input type: {TypeCache<TContext>.ShortName}", nameof(pipe));
    }

    void RemovePipe(TKey key)
    {
        _pipes.TryRemove(key, out _);
    }


    class Handle :
        ConnectHandle
    {
        readonly TKey _key;
        readonly Action<TKey> _removeKey;
        int _disconnected;

        public Handle(TKey key, Action<TKey> removeKey)
        {
            _key = key;
            _removeKey = removeKey;
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 0)
                _removeKey(_key);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
