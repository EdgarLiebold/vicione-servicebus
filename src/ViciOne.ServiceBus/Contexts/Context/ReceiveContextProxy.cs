using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards receive context operations to an underlying context.</summary>
public abstract class ReceiveContextProxy :
    ReceiveContext
{
    readonly ReceiveContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected ReceiveContextProxy(ReceiveContext context)
    {
        _context = context;
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => _context.CancellationToken;

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="contextType">The runtime context type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool HasPayloadType(Type contextType)
    {
        return _context.HasPayloadType(contextType);
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        return _context.TryGetPayload(out payload);
    }

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public virtual TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class
    {
        return _context.GetOrAddPayload(payloadFactory);
    }

    T PipeContext.AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>Gets the publish faults.</summary>
    public bool PublishFaults => _context.PublishFaults;
    /// <summary>Gets the body.</summary>
    public MessageBody Body => _context.Body;

    /// <summary>Gets the elapsed time.</summary>
    public TimeSpan ElapsedTime => _context.ElapsedTime;
    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _context.InputAddress;
    /// <summary>Gets the content type.</summary>
    public ContentType ContentType => _context.ContentType;
    /// <summary>Gets the redelivered.</summary>
    public bool Redelivered => _context.Redelivered;
    /// <summary>Gets the transport headers.</summary>
    public Headers TransportHeaders => _context.TransportHeaders;
    /// <summary>Gets the receive completed.</summary>
    public Task ReceiveCompleted => _context.ReceiveCompleted;
    /// <summary>Gets a value indicating whether delivered.</summary>
    public bool IsDelivered => _context.IsDelivered;
    /// <summary>Gets a value indicating whether faulted.</summary>
    public bool IsFaulted => _context.IsFaulted;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(exception, cancellationToken: cancellationToken);
    }

    /// <summary>Adds receive task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public virtual void AddReceiveTask(Task task)
    {
        _context.AddReceiveTask(task);
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public virtual ISendEndpointProvider SendEndpointProvider => _context.SendEndpointProvider;
    /// <summary>Gets the publish endpoint provider.</summary>
    public virtual IPublishEndpointProvider PublishEndpointProvider => _context.PublishEndpointProvider;
}
