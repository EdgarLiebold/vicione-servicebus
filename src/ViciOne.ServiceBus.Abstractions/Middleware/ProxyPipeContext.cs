using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Forwards pipe-context state and payload operations to a parent context.</summary>
public abstract class ProxyPipeContext
{
    readonly PipeContext _parentContext;

    /// <summary>Initializes a forwarding view over <paramref name="parentContext" />.</summary>
    /// <param name="parentContext">The pipe context to forward.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parentContext" /> is <see langword="null" />.</exception>
    protected ProxyPipeContext(PipeContext parentContext)
    {
        ArgumentNullException.ThrowIfNull(parentContext);

        _parentContext = parentContext;
    }

    /// <inheritdoc cref="PipeContext.CancellationToken" />
    public virtual CancellationToken CancellationToken => _parentContext.CancellationToken;

    /// <inheritdoc cref="PipeContext.HasPayloadType" />
    public virtual bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);

        return payloadType.IsInstanceOfType(this) || _parentContext.HasPayloadType(payloadType);
    }

    /// <inheritdoc cref="PipeContext.TryGetPayload{TPayload}" />
    public virtual bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        if (this is TPayload context)
        {
            payload = context;
            return true;
        }

        return _parentContext.TryGetPayload(out payload);
    }

    /// <inheritdoc cref="PipeContext.GetOrAddPayload{TPayload}" />
    public virtual TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is TPayload context)
            return context;

        return _parentContext.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc cref="PipeContext.AddOrUpdatePayload{TPayload}" />
    public virtual TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory, UpdatePayloadFactory<TPayload> updateFactory)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is TPayload context)
            return context;

        return _parentContext.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
