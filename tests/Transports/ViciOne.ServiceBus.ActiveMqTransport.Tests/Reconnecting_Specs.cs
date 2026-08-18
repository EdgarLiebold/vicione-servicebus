namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;
    using Testing;
    using Transports;


    [TestFixture(ActiveMqHostAddress.ActiveMqScheme)]
    [TestFixture(ActiveMqHostAddress.AmqpScheme)]
    public class Reconnecting_Specs :
        ActiveMqTestFixture
    {
        public Reconnecting_Specs(string protocol)
            : base(protocol)
        {
        }

        /// <summary>
        /// Delivery survives the send endpoint cache turning over, on both protocols.
        /// <para>
        /// The case printed "Okay, restart ActiveMQ" and then looped for twenty seconds while a human
        /// was expected to restart the broker. What it can assert without that human is the mechanism
        /// this fixture actually configures: a send endpoint cache with a two second minimum age and
        /// room for five entries. Enough request/response round trips run to push every cached endpoint
        /// past that age and past that capacity, and a message published afterwards still arrives.
        /// </para>
        /// <para>
        /// Recovery from a broker that really goes away is not covered by this case and is not claimed
        /// anywhere else either: automating it needs container control from inside a test.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_keep_delivering_while_the_send_endpoint_cache_turns_over()
        {
            await Bus.Publish(new ReconnectMessage { Value = "Before" });

            var beforeFound = await Task.Run(() =>
                _consumer.Received.Select<ReconnectMessage>(x => x.Context.Message.Value == "Before").Any());
            Assert.That(beforeFound, Is.True, "the message published before the cache turned over never arrived");

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

            await Bus.Publish(new ReconnectMessage { Value = "After" });

            var afterFound = await Task.Run(() =>
                _consumer.Received.Select<ReconnectMessage>(x => x.Context.Message.Value == "After").Any());
            Assert.That(afterFound, Is.True, "delivery stopped once the send endpoint cache had turned over");
        }

        /// <summary>Six rounds against a cache that holds five entries, so capacity is exceeded.</summary>
        const int CacheTurnoverRounds = 6;

        /// <summary>Half a second each, so six rounds pass the two second minimum age comfortably.</summary>
        static readonly TimeSpan CacheTurnoverInterval = TimeSpan.FromMilliseconds(500);

        public Reconnecting_Specs()
        {
            SendEndpointCacheDefaults.MinAge = TimeSpan.FromSeconds(2);
            SendEndpointCacheDefaults.Capacity = 5;
        }

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
