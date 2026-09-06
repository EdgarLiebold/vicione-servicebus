using System;
using System.Threading;

namespace ViciOne.ServiceBus.Context;

/// <summary>The BindContext.</summary>
/// <typeparam name="TLeft">The left type.</typeparam>
/// <typeparam name="TRight">The right type.</typeparam>
public class BindContextProxy<TLeft, TRight> :
    BindContext<TLeft, TRight>
    where TLeft : class, PipeContext
    where TRight : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="left">The left.</param>
    /// <param name="source">The source value.</param>
    public BindContextProxy(TLeft left, TRight source)
    {
        Left = left;
        Right = source;
    }

    /// <summary>Gets the left.</summary>
    public TLeft Left { get; }

    /// <summary>Gets the right.</summary>
    public TRight Right { get; }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => Left.CancellationToken;

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(Right) || Left.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (Right is T context)
            return context;

        return Left.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (Right is T context)
            return context;

        return Left.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
