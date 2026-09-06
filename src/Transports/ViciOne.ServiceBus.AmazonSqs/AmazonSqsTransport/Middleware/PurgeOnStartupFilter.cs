using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>Purges an Amazon SQS queue once before allowing the client pipeline to continue.</summary>
public class PurgeOnStartupFilter :
    IFilter<ClientContext>
{
    readonly object _lock = new();
    readonly string _queueName;
    Task? _purgeTask;

    /// <summary>Initializes a startup-purge filter.</summary>
    /// <param name="queueName">The logical name of the queue to purge.</param>
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
        await PurgeIfRequestedAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    internal async Task PurgeIfRequestedAsync(ClientContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Task purgeTask;
        lock (_lock)
            purgeTask = _purgeTask ??= PurgeAsync(context);

        try
        {
            await purgeTask.ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
            {
                if (ReferenceEquals(_purgeTask, purgeTask))
                    _purgeTask = null;
            }

            throw;
        }
    }

    async Task PurgeAsync(ClientContext context)
    {
        await context.PurgeQueueAsync(_queueName, context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Purged queue {QueueName}", _queueName);
    }
}
