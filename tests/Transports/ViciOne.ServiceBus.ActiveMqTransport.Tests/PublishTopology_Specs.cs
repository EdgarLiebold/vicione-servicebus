namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using PublishContracts;


    namespace PublishContracts
    {
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
    /// DeployPublishTopology makes the broker hold the topics of the named message types when the bus
    /// starts, and the broker is asked whether it does.
    /// <para>
    /// Both cases had an empty body and were [Explicit], so the deployment was never verified at all.
    /// Measured while filling them in: resolving a topic name through the NMS session leaves the
    /// broker's topic list empty, which is what the product used to call a deployment. Opening a
    /// producer for the resolved destination is what the broker records, and that is what the product
    /// does now.
    /// </para>
    /// <para>
    /// A complete statement is every name the configuration implies and every name it excludes. Two
    /// present names would pass with a deployment that declares whatever it happens to see.
    /// </para>
    /// </summary>
    public class Configuring_the_publish_topology_at_startup :
        ActiveMqTestFixture
    {
        [Test]
        public async Task Should_make_the_broker_hold_the_topics_of_the_named_types()
        {
            IList<string> topics = await Topics();

            Assert.Multiple(() =>
            {
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(OrderSubmitted))),
                    "the topic of a published message type was not deployed at startup");
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(OrderEvent))),
                    "the base type of a published message type carries its own topic and it was not deployed");
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(PackageShipped))),
                    "the topic of the second published message type was not deployed at startup");
            });
        }

        [Test]
        public async Task Should_hold_no_topic_for_a_type_the_configuration_does_not_reach()
        {
            IList<string> topics = await Topics();

            Assert.Multiple(() =>
            {
                Assert.That(topics, Does.Not.Contain(TopicNames.Of(typeof(PackageEvent))),
                    "a base type marked ExcludeFromTopology was deployed anyway");
                Assert.That(topics, Does.Not.Contain(TopicNames.Of(typeof(CustomerEvent))),
                    "a type this configuration never publishes was deployed anyway");
            });
        }

        async Task<IList<string>> Topics()
        {
            (_, IList<string> topics) = await ActiveMqTestHarness.GetBrokerEntities();

            return topics;
        }

        protected override void ConfigureActiveMqBus(IActiveMqBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.Publish<OrderSubmitted>();
            configurator.Publish<PackageShipped>();
        }
    }


    public class Configuring_the_publish_topology_at_startup_by_namespace :
        ActiveMqTestFixture
    {
        [Test]
        public async Task Should_make_the_broker_hold_the_topic_of_every_type_the_scan_finds()
        {
            (_, IList<string> topics) = await ActiveMqTestHarness.GetBrokerEntities();

            // CustomerEvent is what separates a namespace scan from a list of Publish calls: nothing
            // names it, it is the base type of nothing published, and it is in the namespace.
            Assert.Multiple(() =>
            {
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(OrderSubmitted))),
                    "the namespace scan did not deploy the topic of the type it was anchored on");
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(OrderEvent))),
                    "the namespace scan did not deploy the base type in the same namespace");
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(PackageShipped))),
                    "the namespace scan deployed only the anchor type and not the rest of its namespace");
                Assert.That(topics, Does.Contain(TopicNames.Of(typeof(CustomerEvent))),
                    "the namespace scan did not deploy a type that nothing else in the configuration reaches");
            });
        }

        [Test]
        public async Task Should_hold_no_topic_for_an_excluded_type_it_found()
        {
            (_, IList<string> topics) = await ActiveMqTestHarness.GetBrokerEntities();

            // The scan does reach PackageEvent - it is in the namespace it walks - so this is the
            // exclusion taking effect rather than the scan missing it.
            Assert.That(topics, Does.Not.Contain(TopicNames.Of(typeof(PackageEvent))),
                "the namespace scan deployed a base type marked ExcludeFromTopology");
        }

        protected override void ConfigureActiveMqBus(IActiveMqBusFactoryConfigurator configurator)
        {
            configurator.DeployPublishTopology = true;

            configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>();
        }
    }


    static class TopicNames
    {
        /// <summary>
        /// The topic name the broker reports for a message type.
        /// <para>
        /// Built from the type itself rather than from the product's own URN formatter. A test that
        /// derived the name the same way the transport does would agree with the transport whatever
        /// either of them did; this agrees with the broker.
        /// </para>
        /// </summary>
        public static string Of(Type messageType)
        {
            return $"VirtualTopic.{messageType.Namespace}.{messageType.Name}";
        }
    }
}
