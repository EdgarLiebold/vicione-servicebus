using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Middleware;
/// <summary>
/// Purges the queue on startup, only once per filter instance
/// </summary>
public class PurgeOnStartupFilter :
    IFilter<ClientContext>
{
    readonly object _lock = new();
    readonly string _queueName;
    Task? _purgeTask;

    public PurgeOnStartupFilter(string queueName)
    {
        _queueName = queueName;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("purgeOnStartup");
    }

    async Task IFilter<ClientContext>.Send(ClientContext context, IPipe<ClientContext> next)
    {
        await PurgeIfRequested(context).ConfigureAwait(false);

        await next.Send(context).ConfigureAwait(false);
    }

    internal async Task PurgeIfRequested(ClientContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Task purgeTask;
        lock (_lock)
            purgeTask = _purgeTask ??= Purge(context);

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

    async Task Purge(ClientContext context)
    {
        await context.PurgeQueue(_queueName, context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Purged queue {QueueName}", _queueName);
    }
}
