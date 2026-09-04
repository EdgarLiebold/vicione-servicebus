using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scope pipe context implementation.
/// </summary>
public class ScopePipeContext
{
    readonly PipeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>
    /// A pipe using the parent scope cancellationToken
    /// </summary>
    /// <param name="context"></param>
    protected ScopePipeContext(PipeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// A pipe using the parent scope cancellationToken
    /// </summary>
    /// <param name="context"></param>
    /// <param name="payloads">Loads the payload cache with the specified objects</param>
    protected ScopePipeContext(PipeContext context, params object[]? payloads)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        if (payloads != null && payloads.Length > 0)
            _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public virtual CancellationToken CancellationToken => _context.CancellationToken;

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
    public virtual bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

    /// <summary>
    /// Attempts to get payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payload">The payload value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
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
    public virtual T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
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
    public virtual T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
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
