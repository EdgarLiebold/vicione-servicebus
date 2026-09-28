using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.AMQP;
using Apache.NMS.Util;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Serializes access to an Apache NMS session and caches its message producers.</summary>
public class ActiveMqSessionContext :
    ScopePipeContext,
    SessionContext,
    IAsyncDisposable
{
    readonly TaskExecutor _executor;
    readonly MessageProducerCache _messageProducerCache;
    readonly ISession _session;

    /// <summary>Creates a context that owns an Apache NMS session.</summary>
    /// <param name="connectionContext">The owning broker connection context.</param>
    /// <param name="session">The native session owned by the context.</param>
    /// <param name="cancellationToken">The token that signals session-context shutdown.</param>
    public ActiveMqSessionContext(ConnectionContext connectionContext, ISession session, CancellationToken cancellationToken)
        : base(connectionContext)
    {
        ConnectionContext = connectionContext;
        _session = session;
        CancellationToken = cancellationToken;

        _executor = new TaskExecutor();

        _messageProducerCache = new MessageProducerCache();
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that completes after producer, session, and executor cleanup.</returns>
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

    /// <summary>Gets the token that signals session-context shutdown.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the underlying Apache NMS session.</summary>
    public ISession Session => _session;

    /// <summary>Gets the owning ActiveMQ connection context.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Resolves a native topic, using a cached temporary topic when required by the topology.</summary>
    /// <param name="topic">The configured broker topic.</param>
    /// <param name="cancellationToken">The token used to cancel queued topic resolution.</param>
    /// <returns>A task that produces the native topic destination.</returns>
    public Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ITopic>(cancellationToken);

        return _executor.ExecuteAsync(() =>
        {
            var topicName = topic.EntityName.Split('?')[0];

            if (!topic.Durable && topic.AutoDelete
                && topic.EntityName.StartsWith(ConnectionContext.Topology.PublishTopology.VirtualTopicPrefix, StringComparison.InvariantCulture))
                return ConnectionContext.GetTemporaryTopic(_session, topicName);

            return SessionUtil.GetTopic(_session, topicName);
        }, cancellationToken);
    }

    /// <summary>Forces broker-side topic resolution by opening and closing a short-lived producer.</summary>
    /// <param name="topic">The broker topic to resolve.</param>
    /// <param name="cancellationToken">The token used to cancel queued broker-side resolution.</param>
    /// <returns>A task that completes when broker-side resolution has finished.</returns>
    public Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return _executor.ExecuteAsync(() =>
        {
            // Topic resolution and producer creation share the serialized executor because Apache NMS sessions are not thread safe.
            var topicName = topic.EntityName.Split('?')[0];
            ITopic destination = SessionUtil.GetTopic(_session, topicName);

            IMessageProducer producer = _session.CreateProducer(destination);
            try
            {
                producer.Close();
            }
            finally
            {
                // Dispose also runs when Close fails so the session does not retain the short-lived producer.
                producer.Dispose();
            }
        }, cancellationToken);
    }

    /// <summary>Resolves a native queue, using a cached temporary queue when required by the topology.</summary>
    /// <param name="queue">The configured broker queue.</param>
    /// <param name="cancellationToken">The token used to cancel queued queue resolution.</param>
    /// <returns>A task that produces the native queue destination.</returns>
    public Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IQueue>(cancellationToken);

        return _executor.ExecuteAsync(() =>
        {
            if (!queue.Durable && queue.AutoDelete && !ConnectionContext.IsVirtualTopicConsumer(queue.EntityName))
                return ConnectionContext.GetTemporaryQueue(_session, queue.EntityName);

            return SessionUtil.GetQueue(_session, queue.EntityName);
        }, cancellationToken);
    }

    /// <summary>Resolves a cached temporary destination or creates a native destination.</summary>
    /// <param name="destinationName">The destination name.</param>
    /// <param name="destinationType">The Apache NMS destination type.</param>
    /// <param name="cancellationToken">The token used to cancel queued destination resolution.</param>
    /// <returns>A task that produces the native destination.</returns>
    public Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IDestination>(cancellationToken);

        if (ConnectionContext.TryGetTemporaryEntity(destinationName, destinationType, out var destination)
            && destination != null
            && DestinationTypeMatches(destination, destinationType))
            return Task.FromResult(destination);

        return _executor.ExecuteAsync(() => destinationType switch
        {
            DestinationType.TemporaryQueue => ConnectionContext.GetTemporaryQueue(_session, destinationName),
            DestinationType.TemporaryTopic => ConnectionContext.GetTemporaryTopic(_session, destinationName),
            _ => SessionUtil.GetDestination(_session, destinationName, destinationType)
        }, cancellationToken);
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

    /// <summary>Creates a native queue consumer or an appropriately shared and durable topic consumer.</summary>
    /// <param name="destination">The native destination to consume.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    /// <param name="noLocal">Whether messages produced by this connection must be excluded.</param>
    /// <param name="consumerName">The subscription name for a topic consumer.</param>
    /// <param name="shared">Whether a named Artemis AMQP topic subscription is shared.</param>
    /// <param name="durable">Whether a named topic subscription is durable.</param>
    /// <param name="cancellationToken">The token used to cancel queued consumer creation.</param>
    /// <returns>A task that produces the native message consumer.</returns>
    public Task<IMessageConsumer> CreateMessageConsumerAsync(IDestination destination, string? selector, bool noLocal, string? consumerName = null,
        bool shared = false, bool durable = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IMessageConsumer>(cancellationToken);

        return _executor.ExecuteAsync(() =>
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
        }, cancellationToken);
    }

    /// <summary>Sends a native message through the cached producer for a destination.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The Apache NMS message to send.</param>
    /// <param name="cancellationToken">The token used to cancel producer acquisition and sending.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public async Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        var producer = await _messageProducerCache.GetMessageProducerWithCancellationAsync(destination,
            (x, creationToken) => _executor.ExecuteAsync(() => _session.CreateProducerAsync(x), creationToken),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await _executor.ExecuteAsync(() => producer.SendAsync(message, message.NMSDeliveryMode, message.NMSPriority, message.NMSTimeToLive)
            .OrCanceledAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a native byte message in this session.</summary>
    /// <param name="content">The message body bytes.</param>
    /// <returns>The created Apache NMS byte message.</returns>
    public IBytesMessage CreateBytesMessage(byte[] content)
    {
        return _session.CreateBytesMessage(content);
    }

    /// <summary>Creates a native text message in this session.</summary>
    /// <param name="content">The message body text.</param>
    /// <returns>The created Apache NMS text message.</returns>
    public ITextMessage CreateTextMessage(string content)
    {
        return _session.CreateTextMessage(content);
    }

    /// <summary>Creates a native message without a typed body in this session.</summary>
    /// <returns>The created Apache NMS message.</returns>
    public IMessage CreateMessage()
    {
        return _session.CreateMessage();
    }

    /// <summary>Deletes a cached temporary topic or a named broker topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="cancellationToken">The token used to cancel deletion.</param>
    /// <returns>A task that completes when deletion has finished.</returns>
    public Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        TransportLogMessages.DeleteTopic(topicName);

        return _executor.ExecuteAsync(() =>
        {
            if (!ConnectionContext.TryRemoveTemporaryEntity(_session, topicName, DestinationType.TemporaryTopic))
                SessionUtil.DeleteTopic(_session, topicName);
        }, cancellationToken);
    }

    /// <summary>Deletes a cached temporary queue or a named broker queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel deletion.</param>
    /// <returns>A task that completes when deletion has finished.</returns>
    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        TransportLogMessages.DeleteQueue(queueName);

        return _executor.ExecuteAsync(() =>
        {
            if (!ConnectionContext.TryRemoveTemporaryEntity(_session, queueName, DestinationType.TemporaryQueue))
                SessionUtil.DeleteQueue(_session, queueName);
        }, cancellationToken);
    }

    /// <summary>Gets a cached temporary destination by name and destination type.</summary>
    /// <param name="name">The destination name.</param>
    /// <param name="destinationType">The queue or topic destination type.</param>
    /// <returns>The cached destination, or <see langword="null" /> when it is not registered.</returns>
    public IDestination? GetTemporaryDestination(string name, DestinationType destinationType)
    {
        return ConnectionContext.TryGetTemporaryEntity(name, destinationType, out var destination) ? destination : null;
    }
}
