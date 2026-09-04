using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a consume context scope implementation.
/// </summary>
public class ConsumeContextScope :
    ConsumeContextProxy
{
    readonly ConsumeContext _context;
    IPayloadCache? _payloadCache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ConsumeContextScope(ConsumeContext context)
        : base(context.Advanced())
    {
        _context = context;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public ConsumeContextScope(ConsumeContext context, params object[] payloads)
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
            return LazyInitializer.EnsureInitialized(ref _payloadCache, static () => new ListPayloadCache());
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
            return payload;

        if (_context.TryGetPayload(out payload))
            return payload;

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
                return updateFactory(payload);
            }

            return PayloadCache.AddOrUpdatePayload(Add, updateFactory);
        }

        return PayloadCache.AddOrUpdatePayload(addFactory, updateFactory);
    }
}


/// <summary>
/// Provides a consume context scope implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumeContextScope<TMessage> :
    ConsumeContextScope,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ConsumeContextScope(ConsumeContext<TMessage> context)
        : base(context.Advanced())
    {
        _context = context;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public ConsumeContextScope(ConsumeContext<TMessage> context, params object[] payloads)
        : base(context.Advanced(), payloads)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TMessage Message => _context.Message;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
