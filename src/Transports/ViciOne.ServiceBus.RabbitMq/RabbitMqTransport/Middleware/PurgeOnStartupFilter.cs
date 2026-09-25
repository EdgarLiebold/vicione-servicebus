using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>Purges the queue on startup, only once per filter instance.</summary>
public class PurgeOnStartupFilter :
    IFilter<ChannelContext>
{
    readonly string _queueName;
    readonly SemaphoreSlim _purgeGate = new(1, 1);
    bool _queueAlreadyPurged;

    /// <summary>Creates a one-time purge filter for a receive queue.</summary>
    /// <param name="queueName">The queue to inspect and optionally purge.</param>
    public PurgeOnStartupFilter(string queueName)
    {
        _queueName = queueName;
    }

    /// <summary>Adds the one-time startup-purge filter to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("purgeOnStartup");
    }

    /// <summary>Purges a nonempty queue with no active consumers once, then continues the channel pipeline.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="next">The remainder of the channel pipeline.</param>
    /// <returns>A task that completes with the remaining pipeline.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        await PurgeIfRequestedAsync(context, _queueName).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    async Task PurgeIfRequestedAsync(ChannelContext context, string queueName)
    {
        await _purgeGate.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            var queueOk = await context.QueueDeclarePassiveAsync(queueName, context.CancellationToken).ConfigureAwait(false);
            if (queueOk.ConsumerCount != 0 || queueOk.MessageCount == 0)
                return;

            if (_queueAlreadyPurged)
            {
                LogContext.Debug?.Log("Queue {QueueName} was purged at startup, skipping", queueName);
                return;
            }

            var purgedMessageCount = await context.QueuePurgeAsync(queueName, context.CancellationToken).ConfigureAwait(false);
            _queueAlreadyPurged = true;

            LogContext.Debug?.Log("Purged {MessageCount} messages from queue {QueueName}", purgedMessageCount, queueName);
        }
        finally
        {
            _purgeGate.Release();
        }
    }
}
