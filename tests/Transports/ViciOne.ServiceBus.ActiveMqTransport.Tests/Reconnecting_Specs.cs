namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;
    using Testing;
    using Transports;


    /// <summary>
    /// What this fixture proves, and what it does not.
    /// <para>
    /// It proves that delivery survives the send endpoint cache turning over, on both protocols. It
    /// does not prove recovery from a broker that really goes away, and nothing else claims that
    /// either. That capability is blocked on a decision rather than on effort: the analysis is in
    /// evidence/WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03/record-0088/activemq-outage-analysis.json and it
    /// ends at the fixture, not at the transport. An ephemeral published port is rebound when the
    /// container is restarted, so no client could reconnect to the address it holds; pausing the
    /// container keeps the address but the outage is invisible through the port forwarder, so it
    /// cannot be confirmed; disconnecting the network drops the mapping as well. Every remaining
    /// option changes the pinned fixture itself.
    /// </para>
    /// </summary>
    [TestFixture(ActiveMqHostAddress.ActiveMqScheme)]
    [TestFixture(ActiveMqHostAddress.AmqpScheme)]
    public class Reconnecting_Specs :
        ActiveMqTestFixture
    {
        public Reconnecting_Specs(string protocol)
            : base(protocol)
        {
            // The constructor NUnit calls. A parameterised fixture never runs the parameterless one, so
            // settings placed there are settings that do not exist: these two sat in one, and the case
            // below described a cache state that was never established.
            SendEndpointCacheDefaults.MinAge = CacheMinAge;
            SendEndpointCacheDefaults.Capacity = CacheCapacity;
        }

        /// <summary>
        /// Delivery survives the send endpoint cache turning over, on both protocols.
        /// <para>
        /// The configuration this case depends on is asserted before it is used. Without that, the
        /// rounds below turn over a cache of whatever size the process happens to have, and the case
        /// would pass while proving nothing about the mechanism it names.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_keep_delivering_while_the_send_endpoint_cache_turns_over()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SendEndpointCacheDefaults.Capacity, Is.EqualTo(CacheCapacity),
                    "the cache capacity this case depends on was never applied");
                Assert.That(SendEndpointCacheDefaults.MinAge, Is.EqualTo(CacheMinAge),
                    "the cache minimum age this case depends on was never applied");
            });

            await Bus.Publish(new ReconnectMessage { Value = "BeforeTurnover" });

            Assert.That(await Received("BeforeTurnover"), Is.True,
                "the message published before the cache turned over never arrived");

            // More round trips than the cache holds, spread past its minimum age, so entries are both
            // evicted by capacity and aged out rather than only one of the two.
            for (var round = 0; round < CacheTurnoverRounds; round++)
            {
                await Task.Delay(CacheTurnoverInterval, TestCancellationToken);

                var clientFactory = Bus.CreateClientFactory(TestTimeout);

                RequestHandle<PingMessage> request = clientFactory.CreateRequest(new PingMessage());

                Response<PongMessage> response = await request.GetResponse<PongMessage>();

                Assert.That(response.Message, Is.Not.Null, $"round trip {round} produced no response");

                if (clientFactory is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
            }

            await Bus.Publish(new ReconnectMessage { Value = "AfterTurnover" });

            Assert.That(await Received("AfterTurnover"), Is.True,
                "delivery stopped once the send endpoint cache had turned over");
        }

        Task<bool> Received(string value)
        {
            return Task.Run(() =>
                _consumer.Received.Select<ReconnectMessage>(x => x.Context.Message.Value == value).Any());
        }

        /// <summary>Six rounds against a cache that holds five entries, so capacity is exceeded.</summary>
        const int CacheTurnoverRounds = 6;

        /// <summary>Half a second each, so six rounds pass the two second minimum age comfortably.</summary>
        static readonly TimeSpan CacheTurnoverInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>Five entries, so six round trips exceed the capacity.</summary>
        const int CacheCapacity = 5;

        /// <summary>Two seconds, so six rounds of half a second age every entry out.</summary>
        static readonly TimeSpan CacheMinAge = TimeSpan.FromSeconds(2);

        ReconnectConsumer _consumer;

        protected override void ConfigureActiveMqReceiveEndpoint(IActiveMqReceiveEndpointConfigurator configurator)
        {
            base.ConfigureActiveMqReceiveEndpoint(configurator);

            _consumer = new ReconnectConsumer(TestTimeout);

            _consumer.Configure(configurator);

            configurator.Handler<PingMessage>(context => context.RespondAsync(new PongMessage(context.Message.CorrelationId)));
        }


        class ReconnectConsumer :
            MultiTestConsumer
        {
            public ReconnectConsumer(TimeSpan timeout)
                : base(timeout)
            {
                Consume<ReconnectMessage>();
            }
        }


        public class ReconnectMessage
        {
            public string Value { get; set; }
        }
    }
}
