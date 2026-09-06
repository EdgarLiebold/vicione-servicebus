using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Handles the registration of requests and connecting them to the consume pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RequestIdFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>,
    IKeyPipeConnector<TMessage, Guid>
    where TMessage : class
{
    readonly ConcurrentDictionary<Guid, IPipe<ConsumeContext<TMessage>>> _pipes;

    /// <summary>Initializes a new instance.</summary>
    public RequestIdFilter()
    {
        _pipes = new ConcurrentDictionary<Guid, IPipe<ConsumeContext<TMessage>>>();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("key");

        ICollection<IPipe<ConsumeContext<TMessage>>> pipes = _pipes.Values;
        scope.Add("count", pipes.Count);

        foreach (IPipe<ConsumeContext<TMessage>> pipe in pipes)
            pipe.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        Guid? key = context.RequestId;
        if (key.HasValue && _pipes.TryGetValue(key.Value, out IPipe<ConsumeContext<TMessage>>? pipe))
            await pipe.SendAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Connects pipe.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe)
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var added = _pipes.TryAdd(key, pipe);
        if (!added)
            throw new DuplicateKeyPipeConfigurationException($"A pipe with the specified key already exists: {key}");

        return new Handle(key, RemovePipe);
    }

    void RemovePipe(Guid key)
    {
        _pipes.TryRemove(key, out _);
    }


    class Handle :
        ConnectHandle
    {
        readonly Guid _key;
        readonly Action<Guid> _removeKey;

        public Handle(Guid key, Action<Guid> removeKey)
        {
            _key = key;
            _removeKey = removeKey;
        }

        public void Disconnect()
        {
            _removeKey(_key);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
