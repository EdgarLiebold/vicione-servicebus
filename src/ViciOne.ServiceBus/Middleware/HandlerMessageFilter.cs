using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Consumes a message via a message handler and reports the message as consumed or faulted.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class HandlerMessageFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly MessageHandler<TMessage> _handler;
    long _completed;
    long _faulted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public HandlerMessageFilter(MessageHandler<TMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("handler");
        scope.Add("completed", _completed);
        scope.Add("faulted", _faulted);
    }

    [DebuggerNonUserCode]
    async Task IFilter<ConsumeContext<TMessage>>.SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();
        StartedActivity? activity = MessageActivity.TryStartHandler(context);
        var instrument = LogContext.Current?.TryStartHandlerMetrics(context);

        try
        {
            await _handler(context).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<MessageHandler<TMessage>>.ShortName).ConfigureAwait(false);

            Interlocked.Increment(ref _completed);

        }
        catch (Exception exception) when (ConsumerIngressFailure.IsUnexpectedCancellation(exception, context.CancellationToken))
        {
            var canceled = new ConsumerCanceledException(
                $"The operation was canceled by the consumer: {TypeCache<MessageHandler<TMessage>>.ShortName}", exception);
            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<MessageHandler<TMessage>>.ShortName, exception, canceled).ConfigureAwait(false);

            throw canceled;
        }
        catch (Exception ex)
        {
            activity?.AddExceptionEvent(ex);
            instrument?.RecordException(ex);
            Interlocked.Increment(ref _faulted);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<MessageHandler<TMessage>>.ShortName, ex, ex).ConfigureAwait(false);
            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
