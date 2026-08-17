namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TestFramework;


    /// <summary>
    /// What a service instance does to the endpoints of a bus, read back through the bus's own
    /// introspection.
    /// <para>
    /// A service instance is not a job service feature. It binds an endpoint to one running instance,
    /// which is what lets several instances of the same service run side by side: each one gets its own
    /// instance endpoint, while the consumer endpoints keep the shared name they compete on. Both halves
    /// are asserted here, because either one alone would describe something else.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Configuring_endpoints_inside_a_service_instance
    {
        const string InstancePrefix = "Instance_";

        [Test]
        public async Task Should_add_an_instance_endpoint_that_a_plain_bus_does_not_have()
        {
            string[] withInstance = await EndpointNames(useServiceInstance: true);
            string[] plain = await EndpointNames(useServiceInstance: false);

            Assert.Multiple(() =>
            {
                Assert.That(withInstance.Where(name => name.StartsWith(InstancePrefix, StringComparison.Ordinal)),
                    Has.Exactly(1).Items, "the service instance did not get an endpoint of its own");
                Assert.That(plain.Where(name => name.StartsWith(InstancePrefix, StringComparison.Ordinal)),
                    Is.Empty, "a bus without a service instance has an instance endpoint anyway, so the assertion above proves nothing");
            });
        }

        [Test]
        public async Task Should_give_two_competing_instances_endpoint_names_of_their_own()
        {
            var first = Instance(await EndpointNames(useServiceInstance: true));
            var second = Instance(await EndpointNames(useServiceInstance: true));

            Assert.Multiple(() =>
            {
                Assert.That(first, Does.StartWith(InstancePrefix));
                Assert.That(second, Does.StartWith(InstancePrefix));
                Assert.That(second, Is.Not.EqualTo(first),
                    "two service instances share one instance endpoint name, so they would not be isolated");
            });
        }

        [Test]
        public async Task Should_keep_the_consumer_endpoint_name_that_the_instances_compete_on()
        {
            string[] first = await EndpointNames(useServiceInstance: true);
            string[] second = await EndpointNames(useServiceInstance: true);

            Assert.Multiple(() =>
            {
                Assert.That(first, Contains.Item(nameof(ServiceInstanceMessage).Replace("Message", string.Empty)),
                    "the consumer endpoint is not named after the message it takes");
                Assert.That(second, Contains.Item(nameof(ServiceInstanceMessage).Replace("Message", string.Empty)),
                    "the second instance names its consumer endpoint differently, so the two would not compete");
            });
        }

        static string Instance(string[] names)
        {
            string[] matching = names.Where(name => name.StartsWith(InstancePrefix, StringComparison.Ordinal)).ToArray();

            Assert.That(matching, Has.Exactly(1).Items, "expected exactly one instance endpoint");

            return matching[0];
        }

        /// <summary>
        /// Builds a bus with one consumer, once inside a service instance and once without, and returns
        /// the endpoint names the bus reports for itself.
        /// </summary>
        static async Task<string[]> EndpointNames(bool useServiceInstance)
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBus(x =>
                {
                    x.AddConsumer<ServiceInstanceConsumer>();
                    x.UsingInMemory((context, cfg) =>
                    {
                        if (useServiceInstance)
                            cfg.ConfigureServiceInstanceEndpoints(context);
                        else
                            cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var probe = JsonNode.Parse(provider.GetRequiredService<IBusControl>().GetProbeResult().ToJsonString());

            return probe!["results"]!["bus"]!["host"]!["receiveEndpoint"]!.AsArray()
                .Select(endpoint => endpoint!["name"]!.GetValue<string>())
                .ToArray();
        }


        public class ServiceInstanceMessage
        {
        }


        class ServiceInstanceConsumer :
            IConsumer<ServiceInstanceMessage>
        {
            public Task Consume(ConsumeContext<ServiceInstanceMessage> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
