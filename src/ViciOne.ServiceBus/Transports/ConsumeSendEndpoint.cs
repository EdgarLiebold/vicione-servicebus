using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Intercepts the ISendEndpoint and makes it part of the current consume context.</summary>
public class ConsumeSendEndpoint :
    SendEndpointProxy
{
    readonly ConsumeContext _context;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="requestId">The request id.</param>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, cancellationToken));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return ConsumeTaskAsync(base.SendAsync(message, pipe, cancellationToken));
    }

    /// <summary>Gets pipe proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The pipe proxy.</returns>
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
