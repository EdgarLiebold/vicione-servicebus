namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.NMS;
    using Apache.NMS.AMQP;
    using Apache.NMS.Util;
    using Internals;
    using ViciOne.ServiceBus.Middleware;
    using Topology;
    using Transports;
    using Util;


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

            await failures.Capture(
                    () => _messageProducerCache.Stop(CancellationToken.None),
                    exception => LogWarning(exception, "Stop message producers faulted: {Host}"))
                .ConfigureAwait(false);
            await failures.Capture(
                    () => _session.CloseAsync(),
                    exception => LogWarning(exception, "Close session faulted: {Host}"))
                .ConfigureAwait(false);
            failures.Capture(
                () => _session.Dispose(),
                exception => LogWarning(exception, "Dispose session faulted: {Host}"));
            await failures.Capture(
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

        public Task<ITopic> GetTopic(Topic topic)
        {
            return _executor.Run(() =>
            {
                var topicName = topic.EntityName.Split('?')[0];

                if (!topic.Durable && topic.AutoDelete
                    && topic.EntityName.StartsWith(ConnectionContext.Topology.PublishTopology.VirtualTopicPrefix, StringComparison.InvariantCulture))
                    return ConnectionContext.GetTemporaryTopic(_session, topicName);

                return SessionUtil.GetTopic(_session, topicName);
            }, CancellationToken);
        }

        public Task EnsureTopicExists(Topic topic)
        {
            return _executor.Run(() =>
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

        public Task<IQueue> GetQueue(Queue queue)
        {
            return _executor.Run(() =>
            {
                if (!queue.Durable && queue.AutoDelete && !ConnectionContext.IsVirtualTopicConsumer(queue.EntityName))
                    return ConnectionContext.GetTemporaryQueue(_session, queue.EntityName);

                return SessionUtil.GetQueue(_session, queue.EntityName);
            }, CancellationToken);
        }

        public Task<IDestination> GetDestination(string destinationName, DestinationType destinationType)
        {
            if (ConnectionContext.TryGetTemporaryEntity(destinationName, out var destination)
                && DestinationTypeMatches(destination, destinationType))
                return Task.FromResult(destination);

            return _executor.Run(() => SessionUtil.GetDestination(_session, destinationName, destinationType), CancellationToken);
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

        public Task<IMessageConsumer> CreateMessageConsumer(IDestination destination, string selector, bool noLocal, string consumerName = null,
            bool shared = false, bool durable = true)
        {
            return _executor.Run(() =>
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
            var producer = await _messageProducerCache.GetMessageProducer(destination,
                x => _executor.Run(() => _session.CreateProducerAsync(x), cancellationToken)).ConfigureAwait(false);

            await _executor.Run(() => producer.SendAsync(message, message.NMSDeliveryMode, message.NMSPriority, message.NMSTimeToLive)
                .OrCanceled(cancellationToken), cancellationToken).ConfigureAwait(false);
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

        public Task DeleteTopic(string topicName)
        {
            TransportLogMessages.DeleteTopic(topicName);

            return _executor.Run(() =>
            {
                if (!ConnectionContext.TryRemoveTemporaryEntity(_session, topicName))
                    SessionUtil.DeleteTopic(_session, topicName);
            }, CancellationToken.None);
        }

        public Task DeleteQueue(string queueName)
        {
            TransportLogMessages.DeleteQueue(queueName);

            return _executor.Run(() =>
                {
                    if (!ConnectionContext.TryRemoveTemporaryEntity(_session, queueName))
                        SessionUtil.DeleteQueue(_session, queueName);
                }
                , CancellationToken.None);
        }

        public IDestination GetTemporaryDestination(string name)
        {
            return ConnectionContext.TryGetTemporaryEntity(name, out var destination) ? destination : null;
        }
    }
}
