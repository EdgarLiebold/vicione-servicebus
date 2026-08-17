namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TestFramework;


    /// <summary>
    /// The job service configures its own receive endpoints, so the outbox is applied by the product and
    /// not by the test. A test that only sends a job cannot see it. These read the endpoints back out of
    /// the bus and check which middleware actually sits on them.
    /// <para>
    /// Both configuration kinds put the outbox on the three job saga endpoints, and each kind gets it
    /// from a different place, which is what the mutations show: without a container it comes from
    /// JobServiceConfigurator, and with one it comes from the three job saga definitions, each calling
    /// the bus-bound UseInMemoryOutbox(context).
    /// <para>
    /// The scope filter is deliberately not asserted for the container case. Measured with the
    /// UseMessageScope call removed, the endpoints carry the same 32 scope filters either way, because
    /// ConfigureEndpoints(context) already applies the message scope to every endpoint. Asserting it
    /// there would pass whether the job service did its part or not.
    /// </para>
    /// </para>
    /// </summary>
    [TestFixture]
    public class Configuring_the_job_service_endpoints
    {
        static readonly string[] JobEndpoints = { "Job", "JobAttempt", "JobType" };

        [Test]
        public void Should_use_the_outbox_alone_when_there_is_no_registration_context()
        {
            var bus = ViciOne.ServiceBus.Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.ServiceInstance(instance => instance.ConfigureJobServiceEndpoints());
            });

            var filters = FiltersByEndpoint(bus);

            Assert.Multiple(() =>
            {
                foreach (var endpoint in JobEndpoints)
                {
                    Assert.That(filters.Keys, Contains.Item(endpoint));
                    Assert.That(filters[endpoint], Contains.Item("outbox"),
                        $"the {endpoint} endpoint was configured without the outbox");
                    Assert.That(filters[endpoint], Does.Not.Contain("scope"),
                        $"the {endpoint} endpoint rebinds a scoped consume context although there is no container");
                }
            });
        }

        [Test]
        public async Task Should_use_the_outbox_when_a_registration_context_configures_the_endpoints()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBus(x =>
                {
                    x.SetInMemorySagaRepositoryProvider();
                    x.AddJobSagaStateMachines();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseDelayedMessageScheduler();
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var filters = FiltersByEndpoint(provider.GetRequiredService<IBusControl>());

            Assert.Multiple(() =>
            {
                foreach (var endpoint in JobEndpoints)
                {
                    Assert.That(filters.Keys, Contains.Item(endpoint));
                    Assert.That(filters[endpoint], Contains.Item("outbox"),
                        $"the {endpoint} endpoint was configured without the outbox");
                }
            });
        }

        /// <summary>
        /// Reads the bus back through its own introspection instead of trusting the configuration call,
        /// and returns the filter types that actually sit on each receive endpoint.
        /// </summary>
        static Dictionary<string, HashSet<string>> FiltersByEndpoint(IBusControl bus)
        {
            var probe = JsonNode.Parse(bus.GetProbeResult().ToJsonString());
            var endpoints = probe!["results"]!["bus"]!["host"]!["receiveEndpoint"]!.AsArray();

            return endpoints.ToDictionary(
                endpoint => endpoint!["name"]!.GetValue<string>(),
                endpoint => FilterTypes(endpoint, new HashSet<string>(StringComparer.Ordinal)));
        }

        static HashSet<string> FilterTypes(JsonNode node, HashSet<string> found)
        {
            switch (node)
            {
                case JsonObject o:
                    foreach (KeyValuePair<string, JsonNode> property in o)
                    {
                        if (property.Key == "filterType" && property.Value is JsonValue value)
                            found.Add(value.GetValue<string>());
                        else if (property.Value != null)
                            FilterTypes(property.Value, found);
                    }

                    break;
                case JsonArray a:
                    foreach (var item in a)
                    {
                        if (item != null)
                            FilterTypes(item, found);
                    }

                    break;
            }

            return found;
        }
    }
}
