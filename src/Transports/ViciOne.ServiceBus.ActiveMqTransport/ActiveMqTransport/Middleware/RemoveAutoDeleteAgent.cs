namespace ViciOne.ServiceBus.ActiveMqTransport.Middleware;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Middleware;
using Topology;


public sealed class RemoveAutoDeleteAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    public RemoveAutoDeleteAgent(IConnectionContextSupervisor connectionContextSupervisor, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _connectionContextSupervisor = connectionContextSupervisor;

        SetReady();
    }

    protected override async Task StopAgent(StopContext context)
    {
        var failures = new ActiveMqCleanupFailures();
        await failures.Capture(
            () => _connectionContextSupervisor.Send(
                Pipe.ExecuteAsync<ConnectionContext>(async connectionContext =>
                {
                    // Topology setup runs through a scoped session which is released as soon as that
                    // operation completes. Resolve the current connection at stop time: after broker
                    // recovery, the setup connection may already have been retired and must never be
                    // reused for cleanup.
                    var session = await connectionContext.CreateSession(context.CancellationToken).ConfigureAwait(false);
                    await using var sessionContext = new ActiveMqSessionContext(connectionContext, session, context.CancellationToken);
                    await DeleteAutoDelete(sessionContext, failures).ConfigureAwait(false);
                }),
                context.CancellationToken),
            LogCleanupFailure)
            .ConfigureAwait(false);

        await failures.Capture(() => base.StopAgent(context), LogCleanupFailure).ConfigureAwait(false);
        failures.ThrowIfAny("One or more ActiveMQ auto-delete cleanup stages failed.");
    }

    async Task DeleteAutoDelete(SessionContext context, ActiveMqCleanupFailures failures)
    {
        foreach (Func<SessionContext, Task> delete in GetUniqueAutoDeleteOperations())
            await failures.Capture(() => delete(context), LogCleanupFailure).ConfigureAwait(false);
    }

    IEnumerable<Func<SessionContext, Task>> GetUniqueAutoDeleteOperations()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var consumer in _brokerTopology.Consumers.Where(x => x.Destination is not null && x.Destination.AutoDelete))
        {
            var queue = consumer.Destination;
            if (seen.Add($"queue\0{queue.EntityName}"))
                yield return context => Delete(context, queue);
        }

        foreach (var topic in _brokerTopology.Topics.Where(x => x.AutoDelete))
        {
            if (seen.Add($"topic\0{topic.EntityName}"))
                yield return context => Delete(context, topic);
        }

        foreach (var queue in _brokerTopology.Queues.Where(x => x.AutoDelete))
        {
            if (seen.Add($"queue\0{queue.EntityName}"))
                yield return context => Delete(context, queue);
        }
    }

    static void LogCleanupFailure(Exception exception)
    {
        try
        {
            LogContext.Warning?.Log(exception, "Failed to remove one or more subscriptions from the endpoint.");
        }
        catch
        {
            // Cleanup failures remain the product result even if a diagnostic listener fails.
        }
    }

    Task Delete(SessionContext context, Topic topic)
    {
        return context.DeleteTopic(topic.EntityName);
    }

    Task Delete(SessionContext context, Queue queue)
    {
        return context.DeleteQueue(queue.EntityName);
    }
}
