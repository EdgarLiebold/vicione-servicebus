using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a receive context proxy implementation.
/// </summary>
public abstract class ReceiveContextProxy :
    ReceiveContext
{
    readonly ReceiveContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected ReceiveContextProxy(ReceiveContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken => _context.CancellationToken;

    /// <summary>
    /// Determines whether the current value has payload type.
    /// </summary>
    /// <param name="contextType">The context type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool HasPayloadType(Type contextType)
    {
        return _context.HasPayloadType(contextType);
    }

    /// <summary>
    /// Attempts to get payload.
    /// </summary>
    /// <typeparam name="TPayload">The t payload type.</typeparam>
    /// <param name="payload">The payload value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        return _context.TryGetPayload(out payload);
    }

    /// <summary>
    /// Gets or add payload.
    /// </summary>
    /// <typeparam name="TPayload">The t payload type.</typeparam>
    /// <param name="payloadFactory">The payload factory value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class
    {
        return _context.GetOrAddPayload(payloadFactory);
    }

    T PipeContext.AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>
    /// Gets the publish faults value.
    /// </summary>
    public bool PublishFaults => _context.PublishFaults;
    /// <summary>
    /// Gets the body value.
    /// </summary>
    public MessageBody Body => _context.Body;

    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime => _context.ElapsedTime;
    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress => _context.InputAddress;
    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => _context.ContentType;
    /// <summary>
    /// Gets the redelivered value.
    /// </summary>
    public bool Redelivered => _context.Redelivered;
    /// <summary>
    /// Gets the transport headers value.
    /// </summary>
    public Headers TransportHeaders => _context.TransportHeaders;
    /// <summary>
    /// Gets the receive completed value.
    /// </summary>
    public Task ReceiveCompleted => _context.ReceiveCompleted;
    /// <summary>
    /// Gets the is delivered value.
    /// </summary>
    public bool IsDelivered => _context.IsDelivered;
    /// <summary>
    /// Gets the is faulted value.
    /// </summary>
    public bool IsFaulted => _context.IsFaulted;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Adds receive task to the configuration.
    /// </summary>
    /// <param name="task">The task value.</param>
    public virtual void AddReceiveTask(Task task)
    {
        _context.AddReceiveTask(task);
    }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public virtual ISendEndpointProvider SendEndpointProvider => _context.SendEndpointProvider;
    /// <summary>
    /// Gets the publish endpoint provider value.
    /// </summary>
    public virtual IPublishEndpointProvider PublishEndpointProvider => _context.PublishEndpointProvider;
}
