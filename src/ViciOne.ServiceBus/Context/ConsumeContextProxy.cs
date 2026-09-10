using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards consume operations while exposing the proxy itself through payload lookup.</summary>
public abstract class ConsumeContextProxy :
    BaseConsumeContext
{
    readonly ConsumeContext _context;

    /// <summary>Creates a forwarding view over a consume context.</summary>
    /// <param name="context">The consume context to wrap.</param>
    protected ConsumeContextProxy(ConsumeContext context)
        : base((context ?? throw new ArgumentNullException(nameof(context))).ReceiveContext, context.SerializerContext)
    {
        _context = context;
    }

    /// <inheritdoc />
    public override CancellationToken CancellationToken => _context.CancellationToken;

    /// <inheritdoc />
    public override Guid? MessageId => _context.MessageId;
    /// <inheritdoc />
    public override Guid? RequestId => _context.RequestId;
    /// <inheritdoc />
    public override Guid? CorrelationId => _context.CorrelationId;
    /// <inheritdoc />
    public override Guid? ConversationId => _context.ConversationId;
    /// <inheritdoc />
    public override Guid? InitiatorId => _context.InitiatorId;
    /// <inheritdoc />
    public override DateTimeOffset? ExpirationTime => _context.ExpirationTime;
    /// <inheritdoc />
    public override Uri? SourceAddress => _context.SourceAddress;
    /// <inheritdoc />
    public override Uri? DestinationAddress => _context.DestinationAddress;
    /// <inheritdoc />
    public override Uri? ResponseAddress => _context.ResponseAddress;
    /// <inheritdoc />
    public override Uri? FaultAddress => _context.FaultAddress;
    /// <inheritdoc />
    public override DateTimeOffset? SentTime => _context.SentTime;
    /// <inheritdoc />
    public override Headers Headers => _context.Headers;
    /// <inheritdoc />
    public override HostInfo Host => _context.Host;

    /// <inheritdoc />
    public override Task ConsumeCompleted => _context.ConsumeCompleted;

    /// <inheritdoc />
    public override IEnumerable<string> SupportedMessageTypes => _context.SupportedMessageTypes;

    /// <inheritdoc />
    public override bool HasMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return _context.HasMessageType(messageType);
    }

    /// <inheritdoc />
    public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
    {
        if (_context.TryGetMessage(out ConsumeContext<T>? messageContext))
        {
            consumeContext = new MessageConsumeContext<T>(this, messageContext.Message);
            return true;
        }

        consumeContext = null;
        return false;
    }

    /// <inheritdoc />
    public override void AddConsumeTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _context.AddConsumeTask(task);
    }

    /// <inheritdoc />
    public override bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        return payloadType.IsInstanceOfType(this) || _context.HasPayloadType(payloadType);
    }

    /// <inheritdoc />
    public override bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return _context.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is T context)
            return context;

        return _context.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is T context)
            return context;

        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <inheritdoc />
    public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}

/// <summary>Forwards a typed consume context while preserving its message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class ConsumeContextProxy<TMessage> :
    ConsumeContextProxy,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>Creates a forwarding view over a typed consume context.</summary>
    /// <param name="context">The typed consume context to wrap.</param>
    public ConsumeContextProxy(ConsumeContext<TMessage> context)
        : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced())
    {
        _context = context;
    }

    /// <inheritdoc />
    public TMessage Message => _context.Message;

    /// <inheritdoc />
    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
