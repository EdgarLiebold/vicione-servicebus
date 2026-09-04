using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>
/// Purges the queue on startup, only once per filter instance
/// </summary>
public class PurgeOnStartupFilter :
    IFilter<ChannelContext>
{
    readonly string _queueName;
    bool _queueAlreadyPurged;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    public PurgeOnStartupFilter(string queueName)
    {
        _queueName = queueName;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("purgeOnStartup");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        var queueOk = await context.QueueDeclarePassiveAsync(_queueName, context.CancellationToken).ConfigureAwait(false);

        if (queueOk.ConsumerCount == 0 && queueOk.MessageCount > 0)
            await PurgeIfRequestedAsync(context, _queueName).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    async Task PurgeIfRequestedAsync(ChannelContext context, string queueName)
    {
        if (!_queueAlreadyPurged)
        {
            var purgedMessageCount = await context.QueuePurgeAsync(queueName, context.CancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Purged {MessageCount} messages from queue {QueueName}", purgedMessageCount, queueName);

            _queueAlreadyPurged = true;
        }
        else
            LogContext.Debug?.Log("Queue {QueueName} was purged at startup, skipping", queueName);
    }
}
