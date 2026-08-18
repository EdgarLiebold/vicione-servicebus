namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Metadata;
    using NUnit.Framework;
    using PublishContracts;
    using Testing;


    namespace PublishContracts
    {
        using System;


        public interface OrderSubmitted :
            OrderEvent
        {
        }


        public interface OrderEvent
        {
            public Guid OrderId { get; }
        }


        public interface PackageShipped :
            PackageEvent
        {
        }


        [ExcludeFromTopology]
        public interface PackageEvent
        {
        }


        public interface CustomerEvent
        {
            public Guid CustomerId { get; }
        }
    }


    /// <summary>
    /// DeployPublishTopology declares the exchanges of the named message types when the bus starts,
    /// before anything is published. Both cases below had an empty body: they started a bus and
    /// asserted nothing, so they would have passed with the deployment switched off. They read the
    /// exchanges from the broker's own management API now.
    /// </summary>
    public class Configuring_the_publish_topology_at_startup :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_create_the_exchanges()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            Assert.Multiple(() =>
            {
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderSubmitted))),
                    "the exchange of a published message type was not deployed at startup");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(PackageShipped))),
                    "the exchange of the second published message type was not deployed at startup");
            });
        }

        protected override void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.Publish<OrderSubmitted>();
            configurator.Publish<PackageShipped>();
        }
    }


    public class Configuring_the_publish_topology_at_startup_by_namespace :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_create_the_exchanges()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            // The namespace form has to find the types itself, so the second one is what separates it
            // from a single explicit Publish call.
            Assert.Multiple(() =>
            {
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderSubmitted))),
                    "the namespace scan did not deploy the exchange of the type it was anchored on");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(PackageShipped))),
                    "the namespace scan deployed only the anchor type and not the rest of its namespace");
            });
        }

        protected override void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>();
        }
    }


    static class ExchangeNames
    {
        /// <summary>
        /// The exchange name the transport derives from a message type: the message URN without its
        /// scheme, which is what the broker reports.
        /// </summary>
        public static string Of(Type messageType)
        {
            const string scheme = "urn:message:";

            var urn = MessageUrn.ForTypeString(messageType);

            return urn.StartsWith(scheme, StringComparison.Ordinal) ? urn.Substring(scheme.Length) : urn;
        }
    }
}
