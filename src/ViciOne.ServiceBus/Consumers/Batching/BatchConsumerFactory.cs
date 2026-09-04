using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Batching;

/// <summary>
/// Provides a batch consumer factory implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class BatchConsumerFactory<TMessage> :
    IConsumerFactory<BatchConsumer<TMessage>>,
    IAsyncDisposable
    where TMessage : class
{
    readonly IBatchCollector<TMessage> _collector;
    readonly BatchOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="collector">The collector value.</param>
    public BatchConsumerFactory(BatchOptions options, IBatchCollector<TMessage>
        collector)
    {
        _options = options;
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _collector.DisposeAsync();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
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
