using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Consumes a message via a message handler and reports the message as consumed or faulted.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ObserverMessageFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IObserver<ConsumeContext<TMessage>> _observer;
    readonly string _observerType;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="observer">The observer to connect.</param>
    public ObserverMessageFilter(IObserver<ConsumeContext<TMessage>> observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        _observer = observer;
        _observerType = TypeCache.GetShortName(observer.GetType());
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("observer");
        scope.Add("observerType", _observerType);
    }

    [DebuggerNonUserCode]
    async Task IFilter<ConsumeContext<TMessage>>.SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();
        try
        {
            await Task.Yield();

            _observer.OnNext(context);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), _observerType).ConfigureAwait(false);

        }
        catch (Exception ex)
        {
            var failures = new List<Exception> { ex };
            try
            {
                await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), _observerType, ex).ConfigureAwait(false);
            }
            catch (Exception notificationFailure)
            {
                failures.Add(notificationFailure);
            }

            try
            {
                _observer.OnError(ex);
            }
            catch (Exception observerFailure)
            {
                failures.Add(observerFailure);
            }

            if (failures.Count > 1)
                throw new AggregateException(failures);

            throw;
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
