using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.AMQP;
using Apache.NMS.Util;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqSessionContext :
    ScopePipeContext,
    SessionContext,
    IAsyncDisposable
{
    readonly TaskExecutor _executor;
    readonly MessageProducerCache _messageProducerCache;
    readonly ISession _session;

    public ActiveMqSessionContext(ConnectionContext connectionContext, ISession session, CancellationToken cancellationToken)
        : base(connectionContext)
    {
        ConnectionContext = connectionContext;
        _session = session;
        CancellationToken = cancellationToken;

        _executor = new TaskExecutor();

        _messageProducerCache = new MessageProducerCache();
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new ActiveMqCleanupFailures();

        await failures.CaptureAsync(
                () => _messageProducerCache.StopAsync(CancellationToken.None),
                exception => LogWarning(exception, "Stop message producers faulted: {Host}"))
            .ConfigureAwait(false);
        await failures.CaptureAsync(
                () => _session.CloseAsync(),
                exception => LogWarning(exception, "Close session faulted: {Host}"))
            .ConfigureAwait(false);
        failures.Capture(
            () => _session.Dispose(),
            exception => LogWarning(exception, "Dispose session faulted: {Host}"));
        await failures.CaptureAsync(
                () => _executor.DisposeAsync(),
                exception => LogWarning(exception, "Dispose session executor faulted: {Host}"))
            .ConfigureAwait(false);

        failures.ThrowIfAny("One or more ActiveMQ session cleanup stages failed.");
    }

    void LogWarning(Exception exception, string message)
    {
        try
        {
            LogContext.Warning?.Log(exception, message, ConnectionContext.Description);
        }
        catch
        {
            // Cleanup failures remain the product result even if a diagnostic listener fails.
        }
    }

    public override CancellationToken CancellationToken { get; }

    public ISession Session => _session;

    public ConnectionContext ConnectionContext { get; }

    public Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Apache.NMS.ITopic>(cancellationToken); return _executor.ExecuteAsync(() =>
                {
                    var topicName = topic.EntityName.Split('?')[0];

                    if (!topic.Durable && topic.AutoDelete
                        && topic.EntityName.StartsWith(ConnectionContext.Topology.PublishTopology.VirtualTopicPrefix, StringComparison.InvariantCulture))
                        return ConnectionContext.GetTemporaryTopic(_session, topicName);

                    return SessionUtil.GetTopic(_session, topicName);
                }, CancellationToken);
    }

    public Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _executor.ExecuteAsync(() =>
                {
                    // Resolution and the short lived producer belong together and belong here: both touch
                    // the session, which is not thread safe, and this runs while the endpoint is starting.
                    var topicName = topic.EntityName.Split('?')[0];
                    ITopic destination = SessionUtil.GetTopic(_session, topicName);

                    IMessageProducer producer = _session.CreateProducer(destination);
                    try
                    {
                        producer.Close();
                    }
                    finally
                    {
                        // A producer whose close threw is still a producer this session holds.
                        producer.Dispose();
                    }
                }, CancellationToken);
    }

    public Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Apache.NMS.IQueue>(cancellationToken); return _executor.ExecuteAsync(() =>
                {
                    if (!queue.Durable && queue.AutoDelete && !ConnectionContext.IsVirtualTopicConsumer(queue.EntityName))
                        return ConnectionContext.GetTemporaryQueue(_session, queue.EntityName);

                    return SessionUtil.GetQueue(_session, queue.EntityName);
                }, CancellationToken);
    }

    public Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Apache.NMS.IDestination>(cancellationToken); if (ConnectionContext.TryGetTemporaryEntity(destinationName, out var destination)
                    && destination != null
                    && DestinationTypeMatches(destination, destinationType))
            return Task.FromResult(destination);

        return _executor.ExecuteAsync(() => SessionUtil.GetDestination(_session, destinationName, destinationType), CancellationToken);
    }

    bool DestinationTypeMatches(IDestination destination, DestinationType destinationType)
    {
        return destinationType switch
        {
            DestinationType.Queue or DestinationType.TemporaryQueue => destination.IsQueue,
            DestinationType.TemporaryTopic => destination.IsTopic,
            // OpenWire virtual topics route by their canonical VirtualTopic.* name. The AMQP
            // provider instead replaces a TemporaryTopic name with a broker-generated address;
            // publishing must reuse that address or the consumer and producer address different
            // topics. An explicitly requested TemporaryTopic always denotes the registration.
            DestinationType.Topic => destination.IsTopic
                && ConnectionContext.HostAddress.Scheme == ActiveMqHostAddress.AmqpScheme,
            _ => false
        };
    }

    public Task<IMessageConsumer> CreateMessageConsumerAsync(IDestination destination, string? selector, bool noLocal, string? consumerName = null,
        bool shared = false, bool durable = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Apache.NMS.IMessageConsumer>(cancellationToken); return _executor.ExecuteAsync(() =>
                {
                    if (destination.IsTopic && !string.IsNullOrEmpty(consumerName))
                    {
                        if (shared)
                        {
                            if (_session is not NmsSession)
                                throw new NotSupportedException("Shared consumers are supported only on ActiveMQ Artemis broker and with AMQP communication.");

                            return durable
                                ? _session.CreateSharedDurableConsumerAsync((ITopic)destination, consumerName, selector)
                                : _session.CreateSharedConsumerAsync((ITopic)destination, consumerName, selector);
                        }

                        if (durable)
                            return _session.CreateDurableConsumerAsync((ITopic)destination, consumerName, selector);
                    }

                    return _session.CreateConsumerAsync(destination, selector, noLocal);
                }, CancellationToken);
    }

    public async Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        var producer = await _messageProducerCache.GetMessageProducerAsync(destination,
            x => _executor.ExecuteAsync(() => _session.CreateProducerAsync(x), cancellationToken), cancellationToken: cancellationToken).ConfigureAwait(false);

        await _executor.ExecuteAsync(() => producer.SendAsync(message, message.NMSDeliveryMode, message.NMSPriority, message.NMSTimeToLive)
            .OrCanceledAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public IBytesMessage CreateBytesMessage(byte[] content)
    {
        return _session.CreateBytesMessage(content);
    }

    public ITextMessage CreateTextMessage(string content)
    {
        return _session.CreateTextMessage(content);
    }

    public IMessage CreateMessage()
    {
        return _session.CreateMessage();
    }

    public Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); TransportLogMessages.DeleteTopic(topicName);

        return _executor.ExecuteAsync(() =>
        {
            if (!ConnectionContext.TryRemoveTemporaryEntity(_session, topicName))
                SessionUtil.DeleteTopic(_session, topicName);
        }, CancellationToken.None);
    }

    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); TransportLogMessages.DeleteQueue(queueName);

        return _executor.ExecuteAsync(() =>
            {
                if (!ConnectionContext.TryRemoveTemporaryEntity(_session, queueName))
                    SessionUtil.DeleteQueue(_session, queueName);
            }
            , CancellationToken.None);
    }

    public IDestination? GetTemporaryDestination(string name)
    {
        return ConnectionContext.TryGetTemporaryEntity(name, out var destination) ? destination : null;
    }
}
