// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using RabbitMQ.Client;
    using TestFramework;
    using Testing;
    using Transports;


    public abstract class RabbitMqTestFixture :
        BusTestFixture
    {
        TestExecutionContext _fixtureContext;

        protected RabbitMqTestFixture(Uri logicalHostAddress = null, string inputQueueName = null)
            : this(new RabbitMqTestHarness(inputQueueName), logicalHostAddress)
        {
        }

        protected RabbitMqTestFixture(RabbitMqTestHarness harness, Uri logicalHostAddress = null)
            : base(harness)
        {
            RabbitMqTestHarness = harness;

            if (logicalHostAddress != null)
            {
                // The fixture publishes an ephemeral port, so the cluster node has to carry it.
                // A bare host name would resolve to the default 5672, which on a developer machine
                // is very likely a different broker.
                var node = RabbitMqTestHarness.HostAddress;
                RabbitMqTestHarness.NodeHostName = node.IsDefaultPort ? node.Host : $"{node.Host}:{node.Port}";
                RabbitMqTestHarness.HostAddress = logicalHostAddress;
            }

            RabbitMqTestHarness.OnConfigureRabbitMqHost += ConfigureRabbitMqHost;
            RabbitMqTestHarness.OnConfigureRabbitMqBus += ConfigureRabbitMqBus;
            RabbitMqTestHarness.OnConfigureRabbitMqReceiveEndpoint += ConfigureRabbitMqReceiveEndpoint;
            RabbitMqTestHarness.OnCleanupVirtualHost += OnCleanupVirtualHost;
        }

        protected RabbitMqTestHarness RabbitMqTestHarness { get; }

        /// <summary>
        /// The sending endpoint for the InputQueue
        /// </summary>
        protected ISendEndpoint InputQueueSendEndpoint => RabbitMqTestHarness.InputQueueSendEndpoint;

        protected Uri InputQueueAddress => RabbitMqTestHarness.InputQueueAddress;

        protected Uri HostAddress => RabbitMqTestHarness.HostAddress;

        /// <summary>
        /// The sending endpoint for the Bus
        /// </summary>
        protected ISendEndpoint BusSendEndpoint => RabbitMqTestHarness.BusSendEndpoint;

        protected ISentMessageList Sent => RabbitMqTestHarness.Sent;

        protected Uri BusAddress => RabbitMqTestHarness.BusAddress;

        protected IMessageNameFormatter NameFormatter => RabbitMqTestHarness.NameFormatter;

        protected RabbitMqHostSettings GetHostSettings()
        {
            return RabbitMqTestHarness.GetHostSettings();
        }

        [OneTimeSetUp]
        public async Task SetupRabbitMqTestFixture()
        {
            await CleanupVirtualHost().ConfigureAwait(false);

            _fixtureContext = TestExecutionContext.CurrentContext;

            LoggerFactory.Current = _fixtureContext;

            await RabbitMqTestHarness.Start().ConfigureAwait(false);

            await Task.Delay(200);
        }

        [OneTimeTearDown]
        public async Task TearDownRabbitMqTestFixture()
        {
            LoggerFactory.Current = _fixtureContext;

            await RabbitMqTestHarness.Stop().ConfigureAwait(false);

            RabbitMqTestHarness.Dispose();
        }

        protected virtual void ConfigureRabbitMqHost(IRabbitMqHostConfigurator configurator)
        {
        }

        protected virtual void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
        }

        protected virtual void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
        }

        /// <summary>
        /// Resets the broker before the fixture starts.
        /// <para>
        /// This used to delete exchanges and queues individually, and to skip the reset entirely when
        /// the CI environment variable was set. Both were wrong. Deleting entities leaves the
        /// scheduled message store of the delayed message exchange in place, which lets one fixture
        /// affect a later one; and branching on the target environment means the build server ran a
        /// different, weaker isolation than a developer machine.
        /// </para>
        /// <para>
        /// A failure here is no longer written to the error stream and swallowed. A fixture that
        /// starts on a dirty broker produces misleading failures somewhere else entirely.
        /// </para>
        /// </summary>
        async Task CleanupVirtualHost()
        {
            await RabbitMqTestHarness.RecreateVirtualHost().ConfigureAwait(false);
        }

        protected virtual Task OnCleanupVirtualHost(IChannel channel)
        {
            return Task.CompletedTask;
        }
    }
}
