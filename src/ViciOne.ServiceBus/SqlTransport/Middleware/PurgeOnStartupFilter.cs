using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;

/// <summary>
/// Purges the queue on startup, only once per filter instance
/// </summary>
public class PurgeOnStartupFilter :
    IFilter<ClientContext>
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

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("purgeOnStartup");
    }

    async Task IFilter<ClientContext>.SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        await PurgeIfRequestedAsync(context, _queueName).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    async Task PurgeIfRequestedAsync(ClientContext context, string queueName)
    {
        if (!_queueAlreadyPurged)
        {
            await context.PurgeQueueAsync(queueName, context.CancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Purged queue {QueueName}", queueName);

            _queueAlreadyPurged = true;
        }
        else
            LogContext.Debug?.Log("Queue {QueueName} was purged at startup, skipping", queueName);
    }
}
