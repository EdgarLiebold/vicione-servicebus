using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Provides scope-local payloads with fallback to a parent context and its cancellation token.</summary>
public class ScopePipeContext
{
    readonly PipeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>Creates a scope with no local payloads and cancellation inherited from its parent.</summary>
    /// <param name="context">The parent context used for payload fallback and cancellation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    protected ScopePipeContext(PipeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Creates a scope with optional local payloads and cancellation inherited from its parent.</summary>
    /// <param name="context">The parent context used for payload fallback and cancellation.</param>
    /// <param name="payloads">The scope-local payloads, or null to initialize an empty local cache on first use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="payloads" /> contains a <see langword="null" /> element.</exception>
    protected ScopePipeContext(PipeContext context, params object[]? payloads)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        if (payloads != null && payloads.Length > 0)
            _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>Gets the current cancellation token from the parent context.</summary>
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

    /// <summary>Searches the scope itself, local payloads and parent context for a compatible payload type.</summary>
    /// <param name="payloadType">The required runtime payload type.</param>
    /// <returns>Whether the scope or parent provides a compatible payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payloadType" /> is <see langword="null" />.</exception>
    public virtual bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);

        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

    /// <summary>Returns a compatible scope, otherwise searches local payloads before the parent context.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="payload">The compatible payload when found; otherwise, null.</param>
    /// <returns>Whether a compatible payload was found.</returns>
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

    /// <summary>Reuses a compatible scope or local or parent payload, otherwise creates a local payload.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="payloadFactory">Creates a scope-local payload when neither scope nor parent provides one.</param>
    /// <returns>The compatible scope or existing or newly created payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payloadFactory" /> is <see langword="null" />, even when a compatible value exists.</exception>
    public virtual T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is T context)
            return context;

        if (PayloadCache.TryGetPayload<T>(out var payload))
            return payload!;

        if (_context.TryGetPayload(out payload))
            return payload!;

        return PayloadCache.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Returns a compatible scope unchanged, otherwise adds or updates a scope-local payload.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="addFactory">Creates a local payload when neither local nor parent payloads provide one.</param>
    /// <param name="updateFactory">Updates an existing local payload, or projects a parent payload into local storage.</param>
    /// <returns>The compatible scope or the added or updated scope-local payload.</returns>
    /// <exception cref="ArgumentNullException">Either factory is <see langword="null" />, even when a compatible value exists.</exception>
    public virtual T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
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
                return updateFactory(payload!);
            }

            return PayloadCache.AddOrUpdatePayload(Add, updateFactory);
        }

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
