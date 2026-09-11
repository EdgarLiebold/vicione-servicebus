using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Adds an isolated payload scope to a send context while forwarding its send metadata.</summary>
public class SendContextScope :
    SendContextProxy
{
    readonly PipeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>Initializes an empty payload scope over <paramref name="context" />.</summary>
    /// <param name="context">The send context to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public SendContextScope(SendContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Initializes a payload scope over <paramref name="context" />.</summary>
    /// <param name="context">The send context to wrap.</param>
    /// <param name="payloads">The payloads visible from the new scope.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="payloads" /> is <see langword="null" />.</exception>
    public SendContextScope(SendContext context, params object[] payloads)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        _context = context;

        _payloadCache = new ListPayloadCache(payloads);
    }

    /// <inheritdoc />
    public override CancellationToken CancellationToken => _context.CancellationToken;

    IPayloadCache PayloadCache
    {
        get
        {
            if (_payloadCache != null)
                return _payloadCache;

            while (Volatile.Read(ref _payloadCache) == null)
                Interlocked.CompareExchange(ref _payloadCache, new ListPayloadCache(), null);

            return _payloadCache!;
        }
    }

    /// <inheritdoc />
    public override bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);

        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

    /// <inheritdoc />
    public override bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        if (this is TPayload context)
        {
            payload = context;
            return true;
        }

        return PayloadCache.TryGetPayload(out payload) || _context.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public override TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is TPayload context)
            return context;

        if (PayloadCache.TryGetPayload<TPayload>(out var payload))
            return payload!;

        if (_context.TryGetPayload(out payload))
            return payload!;

        return PayloadCache.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public override TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory, UpdatePayloadFactory<TPayload> updateFactory)
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is TPayload context)
            return context;

        if (PayloadCache.TryGetPayload<TPayload>(out var payload))
            return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);

        if (_context.TryGetPayload(out payload))
        {
            TPayload Add()
            {
                return updateFactory(payload!);
            }

            return PayloadCache.AddOrUpdatePayload(Add, updateFactory);
        }

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}


/// <summary>Adds an isolated payload scope to a message-specific send context.</summary>
/// <typeparam name="TMessage">The sent message type.</typeparam>
public sealed class SendContextScope<TMessage> :
    SendContextScope,
    SendContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>Initializes an empty payload scope over <paramref name="context" />.</summary>
    /// <param name="context">The typed send context to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public SendContextScope(SendContext<TMessage> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Initializes a payload scope over <paramref name="context" />.</summary>
    /// <param name="context">The typed send context to wrap.</param>
    /// <param name="payloads">The payloads visible from the new scope.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="payloads" /> is <see langword="null" />.</exception>
    public SendContextScope(SendContext<TMessage> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
    }

    /// <inheritdoc />
    public TMessage Message => _context.Message;
}
