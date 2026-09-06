using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Creates batch consumer instances.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchConsumerFactory<TMessage> :
    IConsumerFactory<BatchConsumer<TMessage>>,
    IAsyncDisposable
    where TMessage : class
{
    readonly IBatchCollector<TMessage> _collector;
    readonly BatchOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="collector">The collector.</param>
    public BatchConsumerFactory(BatchOptions options, IBatchCollector<TMessage>
        collector)
    {
        _options = options;
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _collector.DisposeAsync();
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task SendAsync<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<BatchConsumer<TMessage>, T>> next)
        where T : class
    {
        var messageContext = context as ConsumeContext<TMessage>;
        if (messageContext == null)
            throw new MessageException(typeof(T), $"Expected batch message type: {TypeCache<TMessage>.ShortName}");

        BatchConsumer<TMessage> consumer = await _collector.CollectAsync(messageContext).ConfigureAwait(false);

        try
        {
            await next.SendAsync(new ConsumerConsumeContextProxy<BatchConsumer<TMessage>, T>(context, consumer)).ConfigureAwait(false);
        }
        finally
        {
            if (consumer.IsCompleted)
                await _collector.CompleteAsync(messageContext, consumer).ConfigureAwait(false);
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateConsumerFactoryScope<IConsumer<TMessage>>("batch");

        scope.Add("timeLimit", _options.TimeLimit);
        scope.Add("timeLimitStart", _options.TimeLimitStart);
        scope.Add("messageLimit", _options.MessageLimit);
        scope.Add("concurrencyLimit", _options.ConcurrencyLimit);

        _collector.Probe(scope);
    }
}
