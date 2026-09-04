using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Consumes a message via an existing class instance
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class InstanceMessageFilter<TConsumer, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly TConsumer _instance;
    readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _instancePipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="instancePipe">The instance pipe value.</param>
    public InstanceMessageFilter(TConsumer instance, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> instancePipe)
    {
        _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        _instancePipe = instancePipe ?? throw new ArgumentNullException(nameof(instancePipe));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("instance");
        scope.Add("type", TypeCache<TConsumer>.ShortName);

        _instancePipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    async Task IFilter<ConsumeContext<TMessage>>.SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = LogContext.Current?.StartConsumerActivity<TConsumer, TMessage>(context);
        var instrument = LogContext.Current?.StartConsumeInstrument<TConsumer, TMessage>(context);

        try
        {
            await _instancePipe.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, _instance)).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TConsumer>.ShortName).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when ((exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException)
                                          && !context.CancellationToken.IsCancellationRequested)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TConsumer>.ShortName, exception).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            throw new ConsumerCanceledException($"The operation was canceled by the consumer: {TypeCache<TConsumer>.ShortName}");
        }
        catch (Exception exception)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TConsumer>.ShortName, exception).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
    }
}
