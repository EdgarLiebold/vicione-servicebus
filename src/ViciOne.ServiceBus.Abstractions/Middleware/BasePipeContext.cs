using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Carries pipeline cancellation and lazily initialized supplemental payloads.
/// A compatible request for the context itself takes precedence over its payload cache.
/// </summary>
public abstract class BasePipeContext :
    PipeContext
{
    IPayloadCache? _payloadCache;

    /// <summary>Creates a context without cancellation or initial supplemental payloads.</summary>
    protected BasePipeContext()
    {
        CancellationToken = CancellationToken.None;
    }

    /// <summary>Creates a context without cancellation and with optional initial payloads.</summary>
    /// <param name="payloads">The initial payloads, or null to initialize an empty cache on first use.</param>
    protected BasePipeContext(params object[]? payloads)
    {
        CancellationToken = CancellationToken.None;

        if (payloads != null && payloads.Length > 0)
            _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>Creates a context with the supplied cancellation token and no initial payloads.</summary>
    /// <param name="cancellationToken">The token that cancels the pipeline operation.</param>
    protected BasePipeContext(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    /// <summary>Creates a context with cancellation and optional initial payloads.</summary>
    /// <param name="cancellationToken">The token that cancels the pipeline operation.</param>
    /// <param name="payloads">The initial payloads, or null to initialize an empty cache on first use.</param>
    protected BasePipeContext(CancellationToken cancellationToken, params object[]? payloads)
    {
        CancellationToken = cancellationToken;

        if (payloads != null && payloads.Length > 0)
            _payloadCache = new ListPayloadCache(payloads);
    }

    /// <summary>Creates a context with a required payload cache and no cancellation.</summary>
    /// <param name="payloadCache">The cache that stores supplemental payloads.</param>
    protected BasePipeContext(IPayloadCache payloadCache)
    {
        _payloadCache = payloadCache ?? throw new ArgumentNullException(nameof(payloadCache));

        CancellationToken = CancellationToken.None;
    }

    /// <summary>Creates a context with a payload cache and cancellation token.</summary>
    /// <param name="payloadCache">The supplied cache; a null value is initialized lazily on first payload access.</param>
    /// <param name="cancellationToken">The token that cancels the pipeline operation.</param>
    protected BasePipeContext(IPayloadCache payloadCache, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;

        _payloadCache = payloadCache;
    }

    /// <summary>Gets the supplied cache or atomically initializes an empty cache on first access.</summary>
    protected IPayloadCache PayloadCache
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

    /// <summary>Gets the token that cancels the pipeline operation.</summary>
    public virtual CancellationToken CancellationToken { get; }

    /// <summary>Checks whether the context itself or a cached payload is assignable to a runtime type.</summary>
    /// <param name="payloadType">The required runtime payload type.</param>
    /// <returns>Whether the context or cache provides a compatible payload.</returns>
    public virtual bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType);
    }

    /// <summary>Returns the context itself when compatible, otherwise searches the payload cache.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="payload">The compatible context or cached payload when found; otherwise, null.</param>
    /// <returns>Whether a compatible payload was found.</returns>
    public virtual bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return PayloadCache.TryGetPayload(out payload);
    }

    /// <summary>Returns a compatible context or cached payload, creating a cached payload when absent.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="payloadFactory">Creates the cached payload when no compatible value exists.</param>
    /// <returns>The context itself or the existing or newly cached payload.</returns>
    public virtual T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return PayloadCache.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Returns a compatible context unchanged, otherwise adds or updates a cached payload.</summary>
    /// <typeparam name="T">The required payload type.</typeparam>
    /// <param name="addFactory">Creates the cached payload when none exists.</param>
    /// <param name="updateFactory">Replaces a compatible existing cached payload.</param>
    /// <returns>The context itself or the resulting cached payload.</returns>
    public virtual T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
