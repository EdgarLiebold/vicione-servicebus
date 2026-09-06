using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>The base for any pipe context proxy, optimized to avoid member access.</summary>
public abstract class ProxyPipeContext
{
    readonly PipeContext _parentContext;

    /// <summary>The parent pipe context for this proxy.</summary>
    /// <param name="parentContext">The parent context.</param>
    protected ProxyPipeContext(PipeContext parentContext)
    {
        _parentContext = parentContext;
    }

    /// <summary>Returns the CancellationToken for the context (implicit interface).</summary>
    public virtual CancellationToken CancellationToken => _parentContext.CancellationToken;

    /// <summary>Returns true if the payload type is included with or supported by the context type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || _parentContext.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get the specified payload type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return _parentContext.TryGetPayload(out payload);
    }

    /// <summary>Get or add a payload to the context, using the provided payload factory.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payloadFactory">The payload factory, which is only invoked if the payload is not present.</param>
    /// <returns>The or add payload.</returns>
    public virtual T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _parentContext.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Either adds a new payload, or updates an existing payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="addFactory">The payload factory called if the payload is not present.</param>
    /// <param name="updateFactory">The payload factory called if the payload already exists.</param>
    /// <returns>The t produced by the operation.</returns>
    public virtual T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _parentContext.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
