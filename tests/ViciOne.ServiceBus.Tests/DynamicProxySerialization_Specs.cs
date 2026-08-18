namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Metadata;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// An interface message is carried by a type this library emits at run time, and that emission
    /// used to set <see cref="System.Reflection.TypeAttributes.Serializable"/>.
    /// <para>
    /// That flag only ever meant anything to the formatter based serializers of the old framework,
    /// which are obsolete and which this fork does not ship. Its presence therefore had to be
    /// justified by a measurement rather than by habit, so these specs carry the emitted proxy
    /// through every message body serializer the fork retains and read the emitted metadata itself.
    /// Those serializers are System.Text.Json in its normal and raw shapes, and MessagePack.
    /// </para>
    /// <para>
    /// XML and Protobuf are not message body serializers of their own here, which is not the same as
    /// saying they do not exist: they are payload shapes, and the retained serializers carry them.
    /// XmlPayload_Specs and ProtoBufAsJson_Specs cover exactly that, so the scope of this fixture is
    /// the serializer, not every format a message body can hold.
    /// </para>
    /// <para>
    /// The specs were run once with the flag still emitted and once after it was dropped. The
    /// round trips are green in both runs, which is what "no modern effect" means; only
    /// <see cref="Should_not_carry_the_obsolete_serializable_flag"/> changes its answer, which is
    /// what makes it the discriminating one rather than decoration.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_emitted_interface_proxy
    {
        [Test]
        public void Should_be_a_type_this_library_emits()
        {
            // The control for every spec here: without it the fixture could be measuring an ordinary
            // class and would say nothing at all about the emitted flag.
            var implementation = TypeMetadataCache<ProxiedMessage>.ImplementationType;

            Assert.Multiple(() =>
            {
                Assert.That(implementation.IsInterface, Is.False, "the implementation type is still the interface");
                // A nested contract keeps its declaring type in the emitted name, so the namespace
                // is that prefix plus the fixture, not the prefix alone.
                Assert.That(implementation.Namespace, Does.StartWith("ViciOne.ServiceBus.DynamicInternal"));
                Assert.That(typeof(ProxiedMessage).IsAssignableFrom(implementation));
            });
        }

        /// <summary>
        /// The metadata bit the CLI specification assigns to the serializable flag. It is read as a
        /// number on purpose: naming TypeAttributes.Serializable would raise SYSLIB0050 in this very
        /// spec and force the suppression the section removes, and the bit in the emitted metadata is
        /// the more direct evidence anyway.
        /// </summary>
        const TypeAttributes SerializableFlag = (TypeAttributes)0x00002000;

        [Test]
        public void Should_not_carry_the_obsolete_serializable_flag()
        {
            var implementation = TypeMetadataCache<ProxiedMessage>.ImplementationType;

            Assert.That(implementation.Attributes.HasFlag(SerializableFlag), Is.False,
                "the emitted proxy still carries the serializable flag, which only the retired "
                + "formatter based serializers ever read");
        }

        [Test]
        public void Should_read_the_flag_bit_the_runtime_reads()
        {
            // The control for the bit itself: a wrong constant would make the check above pass for
            // every type there is. A type declared serializable in this assembly must show it.
            Assert.That(typeof(SerializableProbe).Attributes.HasFlag(SerializableFlag), Is.True,
                "the constant does not match the flag the runtime records");
        }


        [Serializable]
        class SerializableProbe
        {
        }

        [Test]
        public async Task Should_round_trip_through_the_json_serializer()
        {
            await AssertRoundTrip(cfg => cfg.UseJsonSerializer());
        }

        [Test]
        public async Task Should_round_trip_through_the_raw_json_serializer()
        {
            await AssertRoundTrip(cfg => cfg.UseRawJsonSerializer());
        }

        [Test]
        public async Task Should_round_trip_through_the_message_pack_serializer()
        {
            await AssertRoundTrip(cfg => cfg.UseMessagePackSerializer());
        }

        /// <summary>
        /// Publishes an interface message, which forces the emitted proxy, and reads the values back
        /// on the consuming side. Comparing the values rather than counting deliveries is deliberate:
        /// a serializer that produced an empty instance would still deliver one message.
        /// </summary>
        static async Task AssertRoundTrip(Action<IInMemoryBusFactoryConfigurator> configureSerializer)
        {
            var identifier = NewId.NextGuid();

            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<ProxiedMessageConsumer>();
                    x.UsingInMemory((context, cfg) =>
                    {
                        configureSerializer(cfg);
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = await provider.StartTestHarness();

            await harness.Bus.Publish<ProxiedMessage>(new { Identifier = identifier, Text = "carried" });

            IReceivedMessage<ProxiedMessage> received = await harness.Consumed.SelectAsync<ProxiedMessage>().First();

            Assert.Multiple(() =>
            {
                Assert.That(received.Context.Message.Identifier, Is.EqualTo(identifier));
                Assert.That(received.Context.Message.Text, Is.EqualTo("carried"));
            });
        }


        public interface ProxiedMessage
        {
            Guid Identifier { get; }
            string Text { get; }
        }


        class ProxiedMessageConsumer :
            IConsumer<ProxiedMessage>
        {
            public Task Consume(ConsumeContext<ProxiedMessage> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
