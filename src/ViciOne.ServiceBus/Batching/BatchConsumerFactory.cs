using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Connects individual message pipelines to the batch consumer that owns their completion.</summary>
/// <typeparam name="TMessage">The message contract collected into batches.</typeparam>
internal sealed class BatchConsumerFactory<TMessage> :
    IConsumerFactory<BatchConsumer<TMessage>>,
    IAsyncDisposable
    where TMessage : class
{
    readonly IBatchCollector<TMessage> _collector;
    readonly BatchOptions _options;

    /// <summary>Creates a factory over the collector owned by one batch registration.</summary>
    /// <param name="options">The effective batch options exposed through probing.</param>
    /// <param name="collector">The collector that owns active batches and terminal cleanup.</param>
    public BatchConsumerFactory(BatchOptions options, IBatchCollector<TMessage> collector)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    /// <summary>Stops admissions, flushes partial batches, and drains accepted deliveries.</summary>
    /// <returns>A task that completes after collector shutdown.</returns>
    public ValueTask DisposeAsync()
    {
        return _collector.DisposeAsync();
    }

    /// <summary>Resolves the owning batch and holds the message pipeline open until that batch completes.</summary>
    /// <typeparam name="TInputMessage">The runtime message contract supplied by the connector.</typeparam>
    /// <param name="context">The message context to collect.</param>
    /// <param name="next">The consumer pipeline that waits on the resolved batch consumer.</param>
    /// <returns>A task that completes with the individual message outcome.</returns>
    public async Task SendAsync<TInputMessage>(ConsumeContext<TInputMessage> context,
        IPipe<ConsumerConsumeContext<BatchConsumer<TMessage>, TInputMessage>> next)
        where TInputMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var messageContext = context as ConsumeContext<TMessage>;
        if (messageContext == null)
            throw new MessageException(typeof(TInputMessage), $"Expected batch message type: {TypeCache<TMessage>.ShortName}");

        BatchConsumer<TMessage> consumer = await _collector.CollectAsync(messageContext).ConfigureAwait(false);

        try
        {
            await next.SendAsync(new ConsumerConsumeContextProxy<BatchConsumer<TMessage>, TInputMessage>(context, consumer)).ConfigureAwait(false);
        }
        finally
        {
            if (consumer.IsCompleted)
                await _collector.CompleteAsync(messageContext, consumer).ConfigureAwait(false);
        }
    }

    /// <summary>Adds the effective batch limits and collector pipeline to the probe graph.</summary>
    /// <param name="context">The probe graph to extend.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateConsumerFactoryScope<IConsumer<TMessage>>("batch");

        scope.Add("timeLimit", _options.TimeLimit);
        scope.Add("timeLimitStart", _options.TimeLimitStart);
        scope.Add("messageLimit", _options.MessageLimit);
        scope.Add("concurrencyLimit", _options.ConcurrencyLimit);

        _collector.Probe(scope);
    }
}
