using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Associates outgoing send tasks and inherited request metadata with a consume context.</summary>
internal sealed class ConsumeSendEndpoint :
    SendEndpointProxy
{
    readonly ConsumeContext _context;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    /// <summary>Initializes a consume-aware endpoint without inheriting the request deadline.</summary>
    /// <param name="endpoint">The destination endpoint being decorated.</param>
    /// <param name="context">The consume context that owns outgoing work and headers.</param>
    /// <param name="requestId">The optional request identifier assigned to outgoing messages.</param>
    internal ConsumeSendEndpoint(ISendEndpoint endpoint, ConsumeContext context, Guid? requestId = default)
        : this(endpoint, context, requestId, false)
    {
    }

    internal ConsumeSendEndpoint(ISendEndpoint endpoint, ConsumeContext context, Guid? requestId,
        bool inheritRequestTimeToLive)
        : base(endpoint)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _requestId = requestId;
        _inheritRequestTimeToLive = inheritRequestTimeToLive;
    }

    internal ConsumeSendEndpoint WithEndpoint(ISendEndpoint endpoint)
    {
        return new ConsumeSendEndpoint(endpoint, _context, _requestId, _inheritRequestTimeToLive);
    }

    /// <inheritdoc />
    public override Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, cancellationToken));
    }

    /// <inheritdoc />
    public override Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <inheritdoc />
    public override Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <summary>Creates the pipe that transfers consume and request metadata to a send context.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="pipe">The optional caller-supplied send pipe.</param>
    /// <returns>The consume-aware send pipe.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ConsumeSendPipeAdapter<T>(_context, pipe, _requestId, _inheritRequestTimeToLive);
    }

    Task ConsumeTaskAsync(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _context.AddConsumeTask(task);
        return task;
    }
}
