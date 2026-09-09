using System;
using System.Threading;

namespace ViciOne.ServiceBus.Context;

/// <summary>Combines a pipeline context with one bound value and exposes both through payload lookup.</summary>
/// <typeparam name="TLeft">The pipeline context type.</typeparam>
/// <typeparam name="TRight">The bound value type.</typeparam>
public class BindContextProxy<TLeft, TRight> :
    BindContext<TLeft, TRight>
    where TLeft : class, PipeContext
    where TRight : class
{
    /// <summary>Creates a bound context from a pipeline context and value.</summary>
    /// <param name="left">The pipeline context that owns cancellation and existing payloads.</param>
    /// <param name="source">The value exposed as the right side and as a payload.</param>
    public BindContextProxy(TLeft left, TRight source)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <inheritdoc />
    public TLeft Left { get; }

    /// <inheritdoc />
    public TRight Right { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken => Left.CancellationToken;

    /// <inheritdoc />
    public bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        return payloadType.IsInstanceOfType(Right) || Left.HasPayloadType(payloadType);
    }

    /// <inheritdoc />
    public bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (Right is T context)
        {
            payload = context;
            return true;
        }

        return Left.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (Right is T context)
            return context;

        return Left.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (Right is T context)
            return context;

        return Left.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
