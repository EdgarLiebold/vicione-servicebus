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

    /// <summary>Creates a forwarding view over a receive context.</summary>
    /// <param name="context">The receive context to wrap.</param>
    protected ReceiveContextProxy(ReceiveContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public CancellationToken CancellationToken => _context.CancellationToken;

    /// <inheritdoc />
    public virtual bool HasPayloadType(Type contextType)
    {
        return _context.HasPayloadType(contextType);
    }

    /// <inheritdoc />
    public virtual bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        return _context.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public virtual TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class
    {
        return _context.GetOrAddPayload(payloadFactory);
    }

    T PipeContext.AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <inheritdoc />
    public bool PublishFaults => _context.PublishFaults;
    /// <inheritdoc />
    public MessageBody Body => _context.Body;

    /// <inheritdoc />
    public TimeSpan ElapsedTime => _context.ElapsedTime;
    /// <inheritdoc />
    public Uri InputAddress => _context.InputAddress;
    /// <inheritdoc />
    public ContentType ContentType => _context.ContentType;
    /// <inheritdoc />
    public bool Redelivered => _context.Redelivered;
    /// <inheritdoc />
    public Headers TransportHeaders => _context.TransportHeaders;
    /// <inheritdoc />
    public Task ReceiveCompleted => _context.ReceiveCompleted;
    /// <inheritdoc />
    public bool IsDelivered => _context.IsDelivered;
    /// <inheritdoc />
    public bool IsFaulted => _context.IsFaulted;

    /// <inheritdoc />
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(exception, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public virtual void AddReceiveTask(Task task)
    {
        _context.AddReceiveTask(task);
    }

    /// <inheritdoc />
    public virtual ISendEndpointProvider SendEndpointProvider => _context.SendEndpointProvider;
    /// <inheritdoc />
    public virtual IPublishEndpointProvider PublishEndpointProvider => _context.PublishEndpointProvider;
}
