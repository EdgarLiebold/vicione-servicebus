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
    /// before anything is published, and it is read back from the broker's own management API.
    /// <para>
    /// A complete statement about a topology is not two names that are present. It is every name the
    /// configuration implies, and every name it excludes: an implemented base type carries its own
    /// exchange, a base type marked <see cref="ExcludeFromTopologyAttribute"/> must carry none, and a
    /// type the configuration never reaches must be absent. Only the two together separate a working
    /// deployment from one that declares whatever it happens to see.
    /// </para>
    /// </summary>
    public class Configuring_the_publish_topology_at_startup :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_create_the_exchanges_of_the_named_types_and_their_included_base_types()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            Assert.Multiple(() =>
            {
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderSubmitted))),
                    "the exchange of a published message type was not deployed at startup");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderEvent))),
                    "the base type of a published message type carries its own exchange and it was not deployed");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(PackageShipped))),
                    "the exchange of the second published message type was not deployed at startup");
            });
        }

        [Test]
        public async Task Should_create_no_exchange_for_a_type_the_configuration_does_not_reach()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            Assert.Multiple(() =>
            {
                Assert.That(exchanges, Does.Not.Contain(ExchangeNames.Of(typeof(PackageEvent))),
                    "a base type marked ExcludeFromTopology was deployed anyway");
                Assert.That(exchanges, Does.Not.Contain(ExchangeNames.Of(typeof(CustomerEvent))),
                    "a type this configuration never publishes was deployed anyway");
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
        public async Task Should_create_the_exchange_of_every_type_the_scan_finds()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            // CustomerEvent is what separates a namespace scan from an explicit Publish call: nothing
            // names it, it is not a base type of anything published, and it is in the namespace.
            Assert.Multiple(() =>
            {
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderSubmitted))),
                    "the namespace scan did not deploy the exchange of the type it was anchored on");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(OrderEvent))),
                    "the namespace scan did not deploy the base type in the same namespace");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(PackageShipped))),
                    "the namespace scan deployed only the anchor type and not the rest of its namespace");
                Assert.That(exchanges, Does.Contain(ExchangeNames.Of(typeof(CustomerEvent))),
                    "the namespace scan did not deploy a type that nothing else in the configuration reaches");
            });
        }

        [Test]
        public async Task Should_create_no_exchange_for_an_excluded_type_it_found()
        {
            IReadOnlyCollection<string> exchanges = await BrokerTopologyProbe.Exchanges(RabbitMqTestHarness);

            // The scan does reach PackageEvent - it is in the namespace it walks - so this is the
            // exclusion taking effect, not the scan missing it.
            Assert.That(exchanges, Does.Not.Contain(ExchangeNames.Of(typeof(PackageEvent))),
                "the namespace scan deployed a base type marked ExcludeFromTopology");
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
