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
        StartedActivity? activity = LogContext.Current?.StartHandlerActivity(context);
        var instrument = LogContext.Current?.StartHandlerInstrument(context);

        try
        {
            await _handler(context).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<MessageHandler<TMessage>>.ShortName).ConfigureAwait(false);

            Interlocked.Increment(ref _completed);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when ((exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException)
                                          && !context.CancellationToken.IsCancellationRequested)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<MessageHandler<TMessage>>.ShortName, exception).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            throw new ConsumerCanceledException($"The operation was canceled by the consumer: {TypeCache<MessageHandler<TMessage>>.ShortName}");
        }
        catch (Exception ex)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<MessageHandler<TMessage>>.ShortName, ex).ConfigureAwait(false);

            activity?.AddExceptionEvent(ex);
            instrument?.RecordException(ex);

            Interlocked.Increment(ref _faulted);
            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
    }
}
