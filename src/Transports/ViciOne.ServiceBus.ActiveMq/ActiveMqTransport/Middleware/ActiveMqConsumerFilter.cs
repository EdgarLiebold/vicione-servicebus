using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq.Middleware;

/// <summary>
/// A filter that uses the model context to create a basic consumer and connect it to the model
/// </summary>
public class ActiveMqConsumerFilter :
    IFilter<SessionContext>
{
    readonly ActiveMqReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ActiveMqConsumerFilter(ActiveMqReceiveEndpointContext context)
    {
        _context = context;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
    }

    async Task IFilter<SessionContext>.SendAsync(SessionContext context, IPipe<SessionContext> next)
    {
        var receiveSettings = context.GetPayload<ReceiveSettings>();

        var executor = new TaskExecutor(receiveSettings.PrefetchCount, receiveSettings.ConcurrentMessageLimit);

        var consumers = new List<Task<ActiveMqConsumer>>
        {
            CreateConsumerAsync(context, new QueueEntity(0, GetReceiveEntityName(receiveSettings), receiveSettings.Durable,
                receiveSettings.AutoDelete), receiveSettings.Selector, executor)
        };

        consumers.AddRange(_context.BrokerTopology.Consumers.Where(x => x.Destination == null).Select(x =>
            CreateConsumerAsync(context, new TopicEntity(0, GetReceiveEntityName(receiveSettings, x.Source.EntityName), x.Source.Durable,
                x.Source.AutoDelete), x.Selector, x.ConsumerName, x.IsShared, receiveSettings.Durable, executor)));

        consumers.AddRange(_context.BrokerTopology.Consumers.Where(x => x.Destination != null).Select(x =>
        {
            Queue destination = x.Destination
                ?? throw new InvalidOperationException("An ActiveMQ queue consumer must reference a queue.");
            return CreateConsumerAsync(context,
                new QueueEntity(0, GetReceiveEntityName(receiveSettings, destination.EntityName), destination.Durable, destination.AutoDelete),
                x.Selector, executor);
        }));

        ActiveMqConsumer[] actualConsumers = await Task.WhenAll(consumers).ConfigureAwait(false);

        var supervisor = CreateConsumerSupervisor(context, actualConsumers);

        await supervisor.Ready.ConfigureAwait(false);

        LogContext.Debug?.Log("Consumers Ready: {InputAddress}", _context.InputAddress);

        _context.AddConsumeAgent(supervisor);

        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        try
        {
            await supervisor.Completed.ConfigureAwait(false);
        }
        finally
        {
            DeliveryMetrics[] consumerMetrics = actualConsumers.Cast<DeliveryMetrics>().ToArray();

            DeliveryMetrics metrics = new CombinedDeliveryMetrics(consumerMetrics.Sum(x => x.DeliveryCount),
                consumerMetrics.Max(x => x.ConcurrentDeliveryCount));

            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.ConcurrentDeliveryCount);

            await executor.DisposeAsync().ConfigureAwait(false);
        }
    }

    string GetReceiveEntityName(ReceiveSettings settings, string? entityName = null)
    {
        return settings.AutoDelete
            ? entityName ?? settings.EntityName
            : $"{entityName ?? settings.EntityName}?consumer.prefetchSize={settings.PrefetchCount}";
    }

    Supervisor CreateConsumerSupervisor(SessionContext context, ActiveMqConsumer[] actualConsumers)
    {
        var supervisor = new ConsumerSupervisor(actualConsumers);

        var connectionStopLock = new object();
        Task? connectionStopTask = null;

        void HandleException(Exception exception)
        {
            lock (connectionStopLock)
            {
                if (connectionStopTask == null || connectionStopTask.IsCompleted)
                    connectionStopTask = StopAfterConnectionExceptionAsync(exception);
            }
        }

        async Task StopAfterConnectionExceptionAsync(Exception exception)
        {
            await Task.Yield();

            try
            {
                await supervisor.StopAsync(exception.Message).ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                LogContext.Warning?.Log(stopException, "Stop Faulted");
            }
        }

        context.ConnectionContext.Connection.ExceptionListener += HandleException;

        supervisor.SetReady();

        supervisor.Completed.GetAwaiter().OnCompleted(() =>
            context.ConnectionContext.Connection.ExceptionListener -= HandleException);

        return supervisor;
    }

    async Task<ActiveMqConsumer> CreateConsumerAsync(SessionContext context, Queue entity, string? selector,
        TaskExecutor executor)
    {
        var queue = await context.GetQueueAsync(entity).ConfigureAwait(false);

        var messageConsumer = await context.CreateMessageConsumerAsync(queue, selector, false).ConfigureAwait(false);

        LogContext.Debug?.Log("Created consumer for {InputAddress}: {Queue}", _context.InputAddress, entity.EntityName);

        var consumer = new ActiveMqConsumer(context, messageConsumer, _context, executor);

        return consumer;
    }

    async Task<ActiveMqConsumer> CreateConsumerAsync(SessionContext context, Topic entity, string? selector,
        string? consumerName, bool shared, bool durable, TaskExecutor executor)
    {
        var topic = await context.GetTopicAsync(entity).ConfigureAwait(false);

        var messageConsumer = await context.CreateMessageConsumerAsync(topic, selector, false, consumerName, shared, durable).ConfigureAwait(false);

        LogContext.Debug?.Log("Created consumer for {InputAddress}: {Topic}", _context.InputAddress, entity.EntityName);

        var consumer = new ActiveMqConsumer(context, messageConsumer, _context, executor);

        return consumer;
    }


    class ConsumerSupervisor :
        Supervisor
    {
        public ConsumerSupervisor(ActiveMqConsumer[] consumers)
        {
            foreach (var consumer in consumers)
            {
                if (IsStopping)
                    return;

                _ = ObserveConsumerCompletionAsync(consumer);
                Add(consumer);
            }
        }

        async Task ObserveConsumerCompletionAsync(ActiveMqConsumer consumer)
        {
            try
            {
                await consumer.Completed.ConfigureAwait(false);

                if (!IsStopping)
                    await this.StopAsync("Consumer stopped, stopping supervisor").ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogContext.Warning?.Log(exception, "Stop Faulted");
            }
        }
    }


    class CombinedDeliveryMetrics :
        DeliveryMetrics
    {
        public CombinedDeliveryMetrics(long deliveryCount, int concurrentDeliveryCount)
        {
            DeliveryCount = deliveryCount;
            ConcurrentDeliveryCount = concurrentDeliveryCount;
        }

        public long DeliveryCount { get; }
        public int ConcurrentDeliveryCount { get; }
    }
}
