namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text.Json.Nodes;
    using NUnit.Framework;
    using Saga;
    using TestFramework;
    using TestFramework.Messages;


    /// <summary>
    /// The configuration of a bus has to be built, not merely accepted. Configuring without ever starting states
    /// only that configuring does not throw.
    ///
    /// The configuration is proven through the probe of the started bus, which reports the pipeline that was
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

            JsonNode probe = await StartAndProbe(busControl);

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

            JsonNode probe = await StartAndProbe(busControl);

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
        static async Task<JsonNode> StartAndProbe(IBusControl busControl)
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await busControl.StartAsync(cancellation.Token);
            try
            {
                return JsonNode.Parse(busControl.GetProbeResult().ToJsonString());
            }
            finally
            {
                await busControl.StopAsync(cancellation.Token);
            }
        }

        /// <summary>
        /// Every filter the built pipeline reports, by the kind it names itself.
        /// </summary>
        static IReadOnlyCollection<string> FilterTypes(JsonNode probe)
        {
            return PropertiesIn(probe)
                .Where(x => x.Key == "filterType")
                .Select(x => x.Value.ToString())
                .ToArray();
        }

        /// <summary>
        /// The limit every filter of the given kind reports, which is the value the configuration asked for.
        /// </summary>
        static IReadOnlyCollection<int> LimitsOf(JsonNode probe, string filterType)
        {
            return NodesIn(probe)
                .OfType<JsonObject>()
                .Where(x => x["filterType"]?.ToString() == filterType && x["limit"] != null)
                .Select(x => x["limit"].GetValue<int>())
                .ToArray();
        }

        static IReadOnlyCollection<string> TextOf(JsonNode probe, string scope, string key)
        {
            return Below(probe, scope)
                .Where(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.ToString())
                .ToArray();
        }

        static IEnumerable<KeyValuePair<string, JsonNode>> Below(JsonNode probe, string scope)
        {
            return PropertiesIn(probe)
                .Where(x => string.Equals(x.Key, scope, StringComparison.Ordinal))
                .Select(x => x.Value)
                .Where(value => value is JsonObject or JsonArray)
                .SelectMany(PropertiesIn);
        }

        /// <summary>
        /// The node itself and everything under it. JsonNode has no descendant walk of its own, and this
        /// walk is the only thing Json.NET was still carrying for these specifications.
        /// </summary>
        static IEnumerable<JsonNode> NodesIn(JsonNode node)
        {
            yield return node;

            switch (node)
            {
                case JsonObject o:
                    foreach (var property in o)
                    {
                        if (property.Value == null)
                            continue;

                        foreach (var nested in NodesIn(property.Value))
                            yield return nested;
                    }

                    break;
                case JsonArray a:
                    foreach (var item in a)
                    {
                        if (item == null)
                            continue;

                        foreach (var nested in NodesIn(item))
                            yield return nested;
                    }

                    break;
            }
        }

        /// <summary>Every property at any depth, with the name it was written under.</summary>
        static IEnumerable<KeyValuePair<string, JsonNode>> PropertiesIn(JsonNode node)
        {
            return NodesIn(node)
                .OfType<JsonObject>()
                .SelectMany(o => o)
                .Where(property => property.Value != null);
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
