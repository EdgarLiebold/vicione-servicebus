using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Consumes a message via Consumer, resolved through the consumer factory and notifies the context that the message was consumed.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class ConsumerMessageFilter<TConsumer, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _consumerPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="consumerPipe">The consumer pipe.</param>
    public ConsumerMessageFilter(IConsumerFactory<TConsumer> consumerFactory, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> consumerPipe)
    {
        _consumerFactory = consumerFactory;
        _consumerPipe = consumerPipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("consumer");
        scope.Add("type", TypeCache<TConsumer>.ShortName);

        _consumerFactory.Probe(scope);

        _consumerPipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    async Task IFilter<ConsumeContext<TMessage>>.SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = MessageActivity.TryStartConsumer<TConsumer, TMessage>(context);
        var instrument = LogContext.Current?.TryStartConsumerMetrics<TConsumer, TMessage>(context);

        try
        {
            await _consumerFactory.SendAsync(context, _consumerPipe).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TConsumer>.ShortName).ConfigureAwait(false);

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

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
