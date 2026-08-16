namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Saga;
    using TestFramework;
    using TestFramework.Messages;


    /// <summary>
    /// The configuration of a bus has to be built, not merely accepted. Both cases used to call
    /// CreateUsingInMemory, drop the returned bus control on the floor and assert nothing, so the only statement
    /// they made was that configuring does not throw, and neither of them ever started or stopped a bus.
    ///
    /// The configuration is now proven through the probe of the started bus, which reports the pipeline that was
    /// actually built, and the bus is stopped deterministically in every case.
    /// </summary>
    [TestFixture]
    public class SimpleConfiguration_Specs
    {
        [Test]
        public async Task Should_build_the_configured_endpoint_pipeline()
        {
            IBusControl busControl = Bus.Factory.CreateUsingInMemory(x =>
            {
                x.UseTransform<PingMessage>(v =>
                {
                });

                x.UseConcurrencyLimit(BusConcurrencyLimit);
                x.UseRateLimit(BusRateLimit);
                x.UseTransaction();
                x.UseMessageRetry(r => r.Immediate(RetryLimit));

                x.ReceiveEndpoint(InputQueueName, e =>
                {
                    e.Saga(new InMemorySagaRepository<SimpleSaga>(), s =>
                    {
                        s.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
                        s.UseRateLimit(BusRateLimit);
                    });

                    e.Consumer<MyConsumer>(c =>
                    {
                        c.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
                        c.UseRateLimit(EndpointRateLimit);
                    });

                    e.Instance(new MyConsumer(), c =>
                    {
                        c.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
                        c.UseRateLimit(EndpointRateLimit);
                    });

                    e.UseTransaction();
                    e.UseConcurrencyLimit(EndpointConcurrencyLimit);
                    e.UseRateLimit(EndpointRateLimit);
                });
            });

            JObject probe = await StartAndProbe(busControl);

            IReadOnlyCollection<string> filters = FilterTypes(probe);

            Assert.Multiple(() =>
            {
                Assert.That(filters, Contains.Item("transform"), "The transform of the bus must be built");
                Assert.That(filters, Contains.Item("transaction"), "The transaction filters must be built");
                Assert.That(filters, Contains.Item("retry"), "The retry policy must be built");
                Assert.That(filters, Contains.Item("saga"), "The saga must be built");
                Assert.That(filters, Contains.Item("instance"), "The instance must be built");

                Assert.That(LimitsOf(probe, "concurrencyLimit"),
                    Is.SupersetOf(new[] { BusConcurrencyLimit, EndpointConcurrencyLimit, ComponentConcurrencyLimit }),
                    "Every configured concurrency limit must appear in the built pipeline");

                Assert.That(LimitsOf(probe, "rateLimit"), Is.SupersetOf(new[] { BusRateLimit, EndpointRateLimit }),
                    "Every configured rate limit must appear in the built pipeline");

                Assert.That(TextOf(probe, "sagaRepository", "persistence"), Contains.Item("memory"),
                    "The in memory saga repository must be the persistence of the saga");

                Assert.That(TextOf(probe, "consumer", "type"), Contains.Item(typeof(MyConsumer).FullName),
                    "The registered consumer must be built");
            });
        }

        [Test]
        public async Task Should_include_concurrent_limit_on_instance()
        {
            IBusControl busControl = Bus.Factory.CreateUsingInMemory(x =>
            {
                x.ReceiveEndpoint(InputQueueName, e =>
                {
                    e.UseConcurrencyLimit(InstanceEndpointConcurrencyLimit);

                    e.Instance(new MyConsumer());
                });
            });

            JObject probe = await StartAndProbe(busControl);

            // The endpoint concurrency limit is applied per configured message type. An instance registers its
            // message types like any other consumer, so the limit has to reach the pipeline of the instance as well.
            Assert.Multiple(() =>
            {
                Assert.That(FilterTypes(probe), Contains.Item("instance"), "The instance must be built");

                Assert.That(LimitsOf(probe, "concurrencyLimit"), Contains.Item(InstanceEndpointConcurrencyLimit),
                    "The endpoint concurrency limit must be built for a consumer registered as an instance");
            });
        }

        const int BusConcurrencyLimit = 3;
        const int BusRateLimit = 1000;
        const int ComponentConcurrencyLimit = 1;
        const int EndpointConcurrencyLimit = 7;
        const int EndpointRateLimit = 100;
        const int InstanceEndpointConcurrencyLimit = 5;
        const int RetryLimit = 3;
        const string InputQueueName = "input_queue";

        /// <summary>
        /// Starts the bus, takes the probe of the built pipeline and stops the bus again. The bus is stopped on
        /// every path, so a failing assertion cannot leave a running bus behind.
        /// </summary>
        static async Task<JObject> StartAndProbe(IBusControl busControl)
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await busControl.StartAsync(cancellation.Token);
            try
            {
                return JObject.Parse(busControl.GetProbeResult().ToJsonString());
            }
            finally
            {
                await busControl.StopAsync(cancellation.Token);
            }
        }

        /// <summary>
        /// Every filter the built pipeline reports, by the kind it names itself.
        /// </summary>
        static IReadOnlyCollection<string> FilterTypes(JObject probe)
        {
            return probe.Descendants()
                .OfType<JProperty>()
                .Where(x => x.Name == "filterType")
                .Select(x => x.Value.ToString())
                .ToArray();
        }

        /// <summary>
        /// The limit every filter of the given kind reports, which is the value the configuration asked for.
        /// </summary>
        static IReadOnlyCollection<int> LimitsOf(JObject probe, string filterType)
        {
            return probe.Descendants()
                .OfType<JObject>()
                .Where(x => (string)x["filterType"] == filterType && x["limit"] != null)
                .Select(x => x["limit"].ToObject<int>())
                .ToArray();
        }

        static IReadOnlyCollection<string> TextOf(JObject probe, string scope, string key)
        {
            return Below(probe, scope)
                .Where(x => string.Equals(x.Name, key, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.ToString())
                .ToArray();
        }

        static IEnumerable<JProperty> Below(JObject probe, string scope)
        {
            return probe.Descendants()
                .OfType<JProperty>()
                .Where(x => string.Equals(x.Name, scope, StringComparison.Ordinal))
                .Select(x => x.Value)
                .OfType<JContainer>()
                .SelectMany(x => x.DescendantsAndSelf().OfType<JProperty>());
        }


        class MyConsumer :
            IConsumer<PingMessage>
        {
            public Task Consume(ConsumeContext<PingMessage> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
