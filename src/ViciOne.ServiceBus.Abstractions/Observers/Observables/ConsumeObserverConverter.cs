using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Converts the object message type to the generic type T and publishes it on the endpoint specified.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConsumeObserverConverter<T> :
    IConsumeObserverConverter
    where T : class
{
    Task IConsumeObserverConverter.PreConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var consumeContext = context as ConsumeContext<T>;
        if (consumeContext == null)
            throw new ArgumentException("Unexpected context type: " + TypeCache.GetShortName(context.GetType()));

        return observer.PreConsumeAsync(consumeContext);
    }

    Task IConsumeObserverConverter.PostConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var consumeContext = context as ConsumeContext<T>;
        if (consumeContext == null)
            throw new ArgumentException("Unexpected context type: " + TypeCache.GetShortName(context.GetType()));

        return observer.PostConsumeAsync(consumeContext);
    }

    Task IConsumeObserverConverter.ConsumeFaultAsync(IConsumeObserver observer, object context, Exception exception, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var consumeContext = context as ConsumeContext<T>;
        if (consumeContext == null)
            throw new ArgumentException("Unexpected context type: " + TypeCache.GetShortName(context.GetType()));

        return observer.ConsumeFaultAsync(consumeContext, exception);
    }
}
