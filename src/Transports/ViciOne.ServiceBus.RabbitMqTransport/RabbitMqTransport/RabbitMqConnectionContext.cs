namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using ViciOne.ServiceBus.Middleware;
    using RabbitMQ.Client;
    using Transports;


    public class RabbitMqConnectionContext :
        BasePipeContext,
        ConnectionContext,
        IAsyncDisposable
    {
        public RabbitMqConnectionContext(IConnection connection, IRabbitMqHostConfiguration hostConfiguration, string description,
            CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Connection = connection;

            Description = description;
            HostAddress = hostConfiguration.HostAddress;

            PublisherConfirmation = hostConfiguration.PublisherConfirmation;
            BatchSettings = hostConfiguration.BatchSettings;
            ContinuationTimeout = hostConfiguration.Settings.ContinuationTimeout;

            Topology = hostConfiguration.Topology;

            StopTimeout = TimeSpan.FromSeconds(30);

            _lifetime = new TransportLifetime("connection", () => connection.Cleanup(200, "Connection Disposed"));
        }

        readonly TransportLifetime _lifetime;

        /// <summary>
        /// The connection's ownership, so the shutdown notification can invalidate it without disposing
        /// a connection that operations are still unwinding out of. Same model as the channel's, and the
        /// same type: a connection that closes underneath a channel being created loses the broker's
        /// reason exactly as a channel did.
        /// </summary>
        internal TransportLifetime Lifetime => _lifetime;

        public IConnection Connection { get; }

        public string Description { get; }
        public Uri HostAddress { get; }
        public bool PublisherConfirmation { get; }

        public BatchSettings BatchSettings { get; }
        public TimeSpan ContinuationTimeout { get; }

        public TimeSpan StopTimeout { get; }

        public IRabbitMqBusTopology Topology { get; }

        public async Task<IChannel> CreateChannel(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            var options = new CreateChannelOptions(PublisherConfirmation, PublisherConfirmation, consumerDispatchConcurrency: concurrentMessageLimit);

            var channel = await Connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);

            channel.ContinuationTimeout = ContinuationTimeout;

            return channel;
        }

        public async Task<ChannelContext> CreateChannelContext(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken)
        {
            using var lease = Lease();

            var channel = await CreateChannel(concurrentMessageLimit, cancellationToken).ConfigureAwait(false);

            return new RabbitMqChannelContext(this, channel, agent, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            TransportLogMessages.DisconnectHost(Description);

            await _lifetime.DisposeAsync().ConfigureAwait(false);

            TransportLogMessages.DisconnectedHost(Description);
        }

        /// <summary>
        /// Takes this connection's lease, or refuses with the reason the connection actually closed for.
        /// </summary>
        TransportLifetime.Lease Lease()
        {
            if (_lifetime.TryLease(out var lease))
                return lease;

            throw _lifetime.NotAvailable();
        }
    }
}
