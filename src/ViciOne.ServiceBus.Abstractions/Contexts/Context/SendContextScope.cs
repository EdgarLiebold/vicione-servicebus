using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a send context scope implementation.
/// </summary>
public class SendContextScope :
    SendContextProxy
{
    readonly PipeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public SendContextScope(SendContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public SendContextScope(SendContext context, params object[] payloads)
        : base(context)
    {
        _context = context;

        _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
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

    /// <summary>
    /// Determines whether the current value has payload type.
    /// </summary>
    /// <param name="payloadType">The payload type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

    /// <summary>
    /// Attempts to get payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payload">The payload value.</param>
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

    /// <summary>
    /// Gets or add payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payloadFactory">The payload factory value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Adds or update payload to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="addFactory">The add factory value.</param>
    /// <param name="updateFactory">The update factory value.</param>
    /// <returns>The result of the operation.</returns>
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


/// <summary>
/// Provides a send context scope implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SendContextScope<TMessage> :
    SendContextScope,
    SendContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public SendContextScope(SendContext<TMessage> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public SendContextScope(SendContext<TMessage> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TMessage Message => _context.Message;
}
