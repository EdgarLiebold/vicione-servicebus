using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;

/// <summary>Purges the queue on startup, only once per filter instance.</summary>
public class PurgeOnStartupFilter :
    IFilter<ClientContext>
{
    readonly SemaphoreSlim _purgeGate;
    readonly string _queueName;
    bool _queueAlreadyPurged;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueName">The queue name.</param>
    public PurgeOnStartupFilter(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        _queueName = queueName;
        _purgeGate = new SemaphoreSlim(1, 1);
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
        await _purgeGate.WaitAsync(context.CancellationToken).ConfigureAwait(false);

        try
        {
            if (_queueAlreadyPurged)
            {
                LogContext.Debug?.Log("Queue {QueueName} was purged at startup, skipping", queueName);
                return;
            }

            await context.PurgeQueueAsync(queueName, context.CancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Purged queue {QueueName}", queueName);

            _queueAlreadyPurged = true;
        }
        finally
        {
            _purgeGate.Release();
        }
    }
}
