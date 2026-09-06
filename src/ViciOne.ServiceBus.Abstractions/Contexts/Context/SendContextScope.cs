using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Context;

/// <summary>Defines the lifetime scope for send context.</summary>
public class SendContextScope :
    SendContextProxy
{
    readonly PipeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public SendContextScope(SendContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="payloads">The payloads.</param>
    public SendContextScope(SendContext context, params object[] payloads)
        : base(context)
    {
        _context = context;

        _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>Gets the cancellation token.</summary>
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

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        if (this is T context)
            return context;

        if (PayloadCache.TryGetPayload<T>(out var payload))
            return payload!;

        if (_context.TryGetPayload(out payload))
            return payload!;

        return PayloadCache.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        if (this is T context)
            return context;

        if (PayloadCache.TryGetPayload<T>(out var payload))
            return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);

        if (_context.TryGetPayload(out payload))
        {
            T Add()
            {
                return updateFactory(payload!);
            }

            return PayloadCache.AddOrUpdatePayload(Add, updateFactory);
        }

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}


/// <summary>Defines the lifetime scope for send context.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SendContextScope<TMessage> :
    SendContextScope,
    SendContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public SendContextScope(SendContext<TMessage> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="payloads">The payloads.</param>
    public SendContextScope(SendContext<TMessage> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
    }

    /// <summary>Gets the message.</summary>
    public TMessage Message => _context.Message;
}
