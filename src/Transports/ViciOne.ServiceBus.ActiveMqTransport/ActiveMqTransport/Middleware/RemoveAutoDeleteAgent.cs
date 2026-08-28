namespace ViciOne.ServiceBus.ActiveMqTransport.Middleware;

using System;
using System.Linq;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Middleware;
using Topology;


public sealed class RemoveAutoDeleteAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ConnectionContext _connectionContext;

    public RemoveAutoDeleteAgent(ConnectionContext connectionContext, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _connectionContext = connectionContext;

        SetReady();
    }

    protected override async Task StopAgent(StopContext context)
    {
        try
        {
            // Topology setup runs through a scoped session which is released as soon as that
            // operation completes. The session supervisor is already stopping when its send agents
            // are stopped, so it cannot create a replacement at this point. The connection is the
            // longer-lived owner: acquire one stop-scoped session, complete every deletion through
            // its serial executor, and close that session before the agent reports completion.
            var session = await _connectionContext.CreateSession(context.CancellationToken).ConfigureAwait(false);
            await using var sessionContext = new ActiveMqSessionContext(_connectionContext, session, context.CancellationToken);
            await DeleteAutoDelete(sessionContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogContext.Warning?.Log(ex, "Failed to remove one or more subscriptions from the endpoint.");
        }

        await base.StopAgent(context);
    }

    async Task DeleteAutoDelete(SessionContext context)
    {
        try
        {
            await Task.WhenAll(_brokerTopology.Consumers.Where(x => x.Destination is not null && x.Destination.AutoDelete)
                    .Select(consumer => Delete(context, consumer.Destination)))
                .ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.Topics.Where(x => x.AutoDelete).Select(topic => Delete(context, topic))).ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.Queues.Where(x => x.AutoDelete).Select(queue => Delete(context, queue))).ConfigureAwait(false);
        }
        catch (NMSException exception)
        {
            LogContext.Debug?.Log(exception, "Connection was closed, auto-delete queues/topics/consumers could not be deleted");
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Failure removing auto-delete queues/topics");
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
