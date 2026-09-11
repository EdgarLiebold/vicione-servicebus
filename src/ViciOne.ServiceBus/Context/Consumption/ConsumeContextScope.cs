using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Context;

/// <summary>Adds an isolated local payload layer to a consume context.</summary>
public class ConsumeContextScope :
    ConsumeContextProxy
{
    readonly ConsumeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>Creates an initially empty payload scope.</summary>
    /// <param name="context">The consume context to wrap.</param>
    public ConsumeContextScope(ConsumeContext context)
        : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced())
    {
        _context = context;
    }

    /// <summary>Creates a payload scope initialized with the supplied values.</summary>
    /// <param name="context">The consume context to wrap.</param>
    /// <param name="payloads">The payloads visible within this scope.</param>
    public ConsumeContextScope(ConsumeContext context, params object[] payloads)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        ArgumentNullException.ThrowIfNull(payloads);

        _payloadCache = new ListPayloadCache(payloads);
    }

    /// <inheritdoc />
    public override CancellationToken CancellationToken => _context.CancellationToken;

    IPayloadCache PayloadCache
    {
        get
        {
            return LazyInitializer.EnsureInitialized(ref _payloadCache, static () => new ListPayloadCache());
        }
    }

    /// <inheritdoc />
    public override bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
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

        return PayloadCache.TryGetPayload(out payload) || _context.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is T context)
            return context;

        if (PayloadCache.TryGetPayload<T>(out var payload))
            return payload;

        if (_context.TryGetPayload(out payload))
            return payload;

        return PayloadCache.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is T context)
            return context;

        if (PayloadCache.TryGetPayload<T>(out var payload))
            return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);

        if (_context.TryGetPayload(out payload))
        {
            T Add()
            {
                return updateFactory(payload);
            }

            return PayloadCache.AddOrUpdatePayload(Add, updateFactory);
        }

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}

/// <summary>Adds an isolated local payload layer to a typed consume context.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class ConsumeContextScope<TMessage> :
    ConsumeContextScope,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>Creates an initially empty typed payload scope.</summary>
    /// <param name="context">The typed consume context to wrap.</param>
    public ConsumeContextScope(ConsumeContext<TMessage> context)
        : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced())
    {
        _context = context;
    }

    /// <summary>Creates a typed payload scope initialized with the supplied values.</summary>
    /// <param name="context">The typed consume context to wrap.</param>
    /// <param name="payloads">The payloads visible within this scope.</param>
    public ConsumeContextScope(ConsumeContext<TMessage> context, params object[] payloads)
        : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced(), payloads)
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
