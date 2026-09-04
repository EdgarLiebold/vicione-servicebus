using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Intercepts the ISendEndpoint and makes it part of the current consume context
/// </summary>
public class ConsumeSendEndpoint :
    SendEndpointProxy
{
    readonly ConsumeContext _context;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="requestId">The request id value.</param>
    public ConsumeSendEndpoint(ISendEndpoint endpoint, ConsumeContext context, Guid? requestId = default)
        : this(endpoint, context, requestId, false)
    {
    }

    internal ConsumeSendEndpoint(ISendEndpoint endpoint, ConsumeContext context, Guid? requestId,
        bool inheritRequestTimeToLive)
        : base(endpoint)
    {
        _context = context;
        _requestId = requestId;
        _inheritRequestTimeToLive = inheritRequestTimeToLive;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, cancellationToken));
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <summary>
    /// Gets pipe proxy.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ConsumeSendPipeAdapter<T>(_context, pipe, _requestId, _inheritRequestTimeToLive);
    }

    Task ConsumeTaskAsync(Task task)
    {
        _context.AddConsumeTask(task);
        return task;
    }
}
