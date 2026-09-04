using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Payloads;

namespace ViciOne.ServiceBus.Context;

public class ConsumeContextScope :
    ConsumeContextProxy
{
    readonly ConsumeContext _context;
    IPayloadCache? _payloadCache;

    public ConsumeContextScope(ConsumeContext context)
        : base(context.Advanced())
    {
        _context = context;
    }

    public ConsumeContextScope(ConsumeContext context, params object[] payloads)
        : base(context)
    {
        _context = context;

        _payloadCache = new ListPayloadCache(payloads);
    }

    public override CancellationToken CancellationToken => _context.CancellationToken;

    IPayloadCache PayloadCache
    {
        get
        {
            return LazyInitializer.EnsureInitialized(ref _payloadCache, static () => new ListPayloadCache());
        }
    }

    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || PayloadCache.HasPayloadType(payloadType) || _context.HasPayloadType(payloadType);
    }

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


public class ConsumeContextScope<TMessage> :
    ConsumeContextScope,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    public ConsumeContextScope(ConsumeContext<TMessage> context)
        : base(context.Advanced())
    {
        _context = context;
    }

    public ConsumeContextScope(ConsumeContext<TMessage> context, params object[] payloads)
        : base(context.Advanced(), payloads)
    {
        _context = context;
    }

    public TMessage Message => _context.Message;

    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
