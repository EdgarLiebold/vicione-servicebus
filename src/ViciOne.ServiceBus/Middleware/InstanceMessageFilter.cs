using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers.Contexts;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Consumes a message via an existing class instance.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InstanceMessageFilter<TConsumer, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly TConsumer _instance;
    readonly IPipe<ConsumerConsumeContext<TConsumer, TMessage>> _instancePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="instancePipe">The instance pipe.</param>
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

        StartedActivity? activity = MessageActivity.TryStartConsumer<TConsumer, TMessage>(context);
        var instrument = LogContext.Current?.TryStartConsumerMetrics<TConsumer, TMessage>(context);

        try
        {
            await _instancePipe.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, _instance)).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TConsumer>.ShortName).ConfigureAwait(false);

        }
        catch (Exception exception) when (ConsumerIngressFailure.IsUnexpectedCancellation(exception, context.CancellationToken))
        {
            var canceled = new ConsumerCanceledException(
                $"The operation was canceled by the consumer: {TypeCache<TConsumer>.ShortName}", exception);
            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<TConsumer>.ShortName, exception, canceled).ConfigureAwait(false);

            throw canceled;
        }
        catch (Exception exception)
        {
            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<TConsumer>.ShortName, exception, exception).ConfigureAwait(false);

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
