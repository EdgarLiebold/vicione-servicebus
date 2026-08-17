namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Buffers;
    using System.Linq;
    using MessagePack;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;
    using ViciOne.ServiceBus.Serialization.MessagePackFormatters;


    /// <summary>
    /// The interface formatter compiled an expression tree on every serialize and every deserialize
    /// call, so every message paid for a compilation. The invokers are cached now.
    /// <para>
    /// The count of compiled invokers is asserted rather than a duration, because a timing comparison
    /// would prove nothing under load and would still pass if the cache were removed on a fast machine.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Caching_the_interface_formatter_invokers
    {
        [Test]
        public void Should_compile_nothing_further_once_a_type_has_been_seen()
        {
            // One round trip warms both directions: a serialize invoker for the concrete type and the
            // deserialize invoker of the interface. What has to hold is that everything after that is
            // free, so the assertion is zero rather than a small number nobody can justify.
            RoundTrip(new Cached { Id = 0, Name = "warm up" });

            var before = InterfaceMessagePackFormatter<ICached>.CompiledInvokerCount;

            for (var i = 1; i <= 50; i++)
                RoundTrip(new Cached { Id = i, Name = "repeated" });

            Assert.That(InterfaceMessagePackFormatter<ICached>.CompiledInvokerCount - before, Is.Zero,
                "Compilations are counted, not cache entries: a version that rebuilt on every call and "
                + "overwrote the same key would leave the entry count at one and keep this green.");
        }

        [Test]
        public void Should_keep_serializing_correctly_through_the_cached_invoker()
        {
            // A cache that returns a wrong invoker would still be fast. The values have to survive.
            var round = RoundTrip(new Cached { Id = 27, Name = "Frank" });

            Assert.Multiple(() =>
            {
                Assert.That(round.Id, Is.EqualTo(27));
                Assert.That(round.Name, Is.EqualTo("Frank"));
            });
        }

        [Test]
        public void Should_stay_correct_when_many_threads_serialize_at_once()
        {
            // GetOrAdd may run the factory more than once under contention; what must hold is that every
            // caller gets a working invoker and the same values come back.
            ICached[] results = Enumerable.Range(0, 64)
                .AsParallel()
                .WithDegreeOfParallelism(8)
                .Select(i => RoundTrip(new Cached { Id = i, Name = "parallel" }))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(results, Has.Length.EqualTo(64));
                Assert.That(results.Select(x => x.Id).OrderBy(x => x), Is.EqualTo(Enumerable.Range(0, 64)));
                Assert.That(results.All(x => x.Name == "parallel"), Is.True);
            });
        }

        static ICached RoundTrip(ICached message)
        {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance)
                .WithSecurity(MessagePackSecurity.UntrustedData);

            var formatter = new InterfaceMessagePackFormatter<ICached>();

            var writer = new ArrayBufferWriter<byte>();
            var messagePackWriter = new MessagePackWriter(writer);
            formatter.Serialize(ref messagePackWriter, message, options);
            messagePackWriter.Flush();

            var reader = new MessagePackReader(writer.WrittenMemory);

            return formatter.Deserialize(ref reader, options);
        }


        public interface ICached
        {
            int Id { get; }
            string? Name { get; }
        }


        public class Cached :
            ICached
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}
