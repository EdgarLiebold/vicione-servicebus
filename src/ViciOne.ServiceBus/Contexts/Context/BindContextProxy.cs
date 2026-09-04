using System;
using System.Threading;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// The BindContext
/// </summary>
/// <typeparam name="TLeft"></typeparam>
/// <typeparam name="TRight"></typeparam>
public class BindContextProxy<TLeft, TRight> :
    BindContext<TLeft, TRight>
    where TLeft : class, PipeContext
    where TRight : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="source">The source value.</param>
    public BindContextProxy(TLeft left, TRight source)
    {
        Left = left;
        Right = source;
    }

    /// <summary>
    /// Gets the left value.
    /// </summary>
    public TLeft Left { get; }

    /// <summary>
    /// Gets the right value.
    /// </summary>
    public TRight Right { get; }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken => Left.CancellationToken;

    /// <summary>
    /// Determines whether the current value has payload type.
    /// </summary>
    /// <param name="payloadType">The payload type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(Right) || Left.HasPayloadType(payloadType);
    }

    /// <summary>
    /// Attempts to get payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payload">The payload value.</param>
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

    /// <summary>
    /// Gets or add payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payloadFactory">The payload factory value.</param>
    /// <returns>The result of the operation.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (Right is T context)
            return context;

        return Left.GetOrAddPayload(payloadFactory);
    }

    /// <summary>
    /// Adds or update payload to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="addFactory">The add factory value.</param>
    /// <param name="updateFactory">The update factory value.</param>
    /// <returns>The result of the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (Right is T context)
            return context;

        return Left.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
