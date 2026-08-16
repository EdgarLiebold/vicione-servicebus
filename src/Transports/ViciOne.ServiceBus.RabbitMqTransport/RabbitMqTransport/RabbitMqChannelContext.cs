namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using ViciOne.ServiceBus.Middleware;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using Transports;


    public class RabbitMqChannelContext :
        ScopePipeContext,
        ChannelContext,
        IAsyncDisposable
    {
        readonly IAgent _agent;
        readonly CancellationToken _cancellationToken;
        readonly IChannel _channel;

        /// <summary>
        /// Owns the channel. Every operation below runs under a lease from it, so the channel is not
        /// disposed while one is still unwinding — which is what used to replace the broker's answer
        /// with an ObjectDisposedException.
        /// </summary>
        readonly TransportLifetime _lifetime;

        CancellationTokenSource _tokenSource;

        public RabbitMqChannelContext(ConnectionContext connectionContext, IChannel channel, IAgent agent, CancellationToken cancellationToken)
            : base(connectionContext)
        {
            ConnectionContext = connectionContext;

            _channel = channel;
            _lifetime = new TransportLifetime("channel", () => channel.Cleanup(200, "ChannelContext Disposed"));
            _agent = agent;

            _cancellationToken = cancellationToken;
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectionContext.CancellationToken, cancellationToken);
        }

        public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

        public IChannel Channel => _channel;

        internal TransportLifetime Lifetime => _lifetime;

        public ConnectionContext ConnectionContext { get; }

        public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
            CancellationToken cancellationToken)
        {
            // The lease is held until the client's own task finishes, not until this method returns.
            // With publisher confirms off the caller does not wait for the broker, but the client still
            // does — and releasing before that would let the channel be disposed underneath it, which is
            // the very race this ownership exists to prevent. The task is observed on both paths, so a
            // publish nobody waits for still cannot become an unobserved exception.
            var lease = Lease();

            Task publish;
            try
            {
                publish = _channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, new ReadOnlyMemory<byte>(body),
                    cancellationToken).AsTask();
            }
            catch
            {
                lease.Dispose();
                throw;
            }

            async Task PublishAndRelease()
            {
                try
                {
                    await publish.ConfigureAwait(false);
                }
                catch (PublishException exception) when (exception.IsReturn)
                {
                    throw new MessageReturnedException("The message was returned by RabbitMQ", exception);
                }
                catch (PublishException exception)
                {
                    throw new RabbitMqConnectionException("BasicPublishAsync failed", exception);
                }
                finally
                {
                    lease.Dispose();
                }
            }

            var published = PublishAndRelease();

            if (awaitAck)
                return published;

            published.IgnoreUnobservedExceptions();

            return Task.CompletedTask;
        }


        public async Task ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments,
            CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.ExchangeBindAsync(destination, source, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments,
            CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task ExchangeDeclarePassive(string exchange, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.ExchangeDeclarePassiveAsync(exchange, cancellationToken).ConfigureAwait(false);
        }

        public async Task QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.QueueBindAsync(queue, exchange, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<QueueDeclareOk> QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete,
            IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            return await _channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<QueueDeclareOk> QueueDeclarePassive(string queue, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            return await _channel.QueueDeclarePassiveAsync(queue, cancellationToken).ConfigureAwait(false);
        }

        public async Task<uint> QueuePurge(string queue, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            return await _channel.QueuePurgeAsync(queue, cancellationToken).ConfigureAwait(false);
        }

        public async Task BasicQos(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.BasicQosAsync(prefetchSize, prefetchCount, global, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask BasicAck(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
        {
            // The old form asserted the broker had closed the channel with reply code 491 — a code the
            // broker never sends. A caller now learns the real reason, or this transport's own where
            // there is none.
            using var lease = Lease();

            await _channel.BasicAckAsync(deliveryTag, multiple, cancellationToken).ConfigureAwait(false);
        }

        public async Task BasicNack(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
        {
            // A nack on a channel that is finished is not an error: shutting down, the broker requeues
            // the prefetched messages anyway. So the refused lease is answered with silence here, unlike
            // an acknowledgement, where the caller has to learn that it did not happen.
            if (!_lifetime.TryLease(out var lease))
                return;

            using (lease)
            {
                try
                {
                    await _channel.BasicNackAsync(deliveryTag, multiple, requeue, cancellationToken).ConfigureAwait(false);
                }
                catch (AlreadyClosedException)
                {
                }
            }
        }

        public async Task<string> BasicConsume(string queue, bool noAck, bool exclusive, IDictionary<string, object> arguments,
            IAsyncBasicConsumer consumer, string consumerTag, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            return await _channel.BasicConsumeAsync(queue, noAck, consumerTag, false, exclusive, arguments, consumer, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task BasicCancel(string consumerTag, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            await _channel.BasicCancelAsync(consumerTag, false, cancellationToken).ConfigureAwait(false);
        }

        public void NotifyFaulted(Exception exception, Uri inputAddress)
        {
            Task.Run(() => _agent.Stop($"Unrecoverable exception on {inputAddress.GetEndpointName()}", CancellationToken.None), CancellationToken.None)
                .IgnoreUnobservedExceptions();
        }

        /// <summary>
        /// Claims the channel for one operation, or says why it is gone.
        /// <para>
        /// The refusal carries the broker's own close reason when there is one. That is what keeping it
        /// is for: a caller arriving after the channel closed learns what closed it, rather than an
        /// invented answer or an ObjectDisposedException from a channel pulled out from under it.
        /// </para>
        /// </summary>
        TransportLifetime.Lease Lease()
        {
            if (_lifetime.TryLease(out var lease))
                return lease;

            throw _lifetime.NotAvailable();
        }

        public async ValueTask DisposeAsync()
        {
            // Waits for the operations still running. Disposing while one of them is unwinding is the
            // defect this ownership exists to prevent.
            await _lifetime.DisposeAsync().ConfigureAwait(false);

            _tokenSource?.Dispose();
            _tokenSource = null;
        }
    }
}
