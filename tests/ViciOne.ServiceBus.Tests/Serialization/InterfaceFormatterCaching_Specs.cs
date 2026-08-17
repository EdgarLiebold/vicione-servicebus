namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Buffers;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using MessagePack;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization.MessagePackFormatters;


    /// <summary>
    /// The interface formatter compiled an expression tree, closed a generic resolver method and invoked
    /// a <see cref="MethodInfo" /> on every serialize and every deserialize call, so every message paid
    /// for all three. They are compiled once per concrete type now.
    /// <para>
    /// The count of compiled entries is asserted rather than a duration, because a timing comparison
    /// would prove nothing under load and would still pass with the cache removed on a fast machine.
    /// </para>
    /// <para>
    /// Each case builds its own cache. My previous version asserted a static counter, so a first use was
    /// only a first use if no earlier test in the run had already warmed the same static state, and the
    /// concurrency case could measure a cache that was already full.
    /// </para>
    /// <para>
    /// The weak key cases below say that this table does not hold its own key. They are not an unload
    /// statement: the architecture does not promise in-process module unload, and the dependency roots a
    /// runtime generated type on its own. That observation is recorded in the evidence rather than as a
    /// required test, because a required test asserting a dependency defect would fail the build the day
    /// the dependency improves.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Caching_the_interface_formatter_invokers
    {
        [Test]
        public void Should_compile_once_and_never_again_for_the_same_type()
        {
            var cache = new ConcreteFormatterCache<ICached>();

            for (var i = 0; i < 100; i++)
                cache.Get(typeof(Cached));

            Assert.That(cache.CompiledCount, Is.EqualTo(1),
                "compilations are counted, not entries: a version that rebuilt on every call and replaced "
                + "the same entry would leave the entry count at one and keep this green");
        }

        [Test]
        public void Should_compile_once_when_many_threads_arrive_on_a_cold_cache_together()
        {
            var cache = new ConcreteFormatterCache<ICached>();
            var accesses = new ConcreteFormatterAccess<ICached>[16];

            using (var start = new ManualResetEventSlim(false))
            {
                Thread[] threads = Enumerable.Range(0, accesses.Length)
                    .Select(index => new Thread(() =>
                    {
                        start.Wait();
                        accesses[index] = cache.Get(typeof(Cached));
                    }))
                    .ToArray();

                foreach (var thread in threads)
                    thread.Start();

                start.Set();

                foreach (var thread in threads)
                    thread.Join();
            }

            Assert.Multiple(() =>
            {
                Assert.That(cache.CompiledCount, Is.EqualTo(1), "a losing thread has to wait, not compile a second time");
                Assert.That(accesses, Is.All.Not.Null);
                Assert.That(accesses.Distinct().Count(), Is.EqualTo(1), "every caller ends up on the same entry");
            });
        }

        [Test]
        public void Should_compile_one_entry_per_concrete_type()
        {
            var cache = new ConcreteFormatterCache<ICached>();

            cache.Get(typeof(Cached));
            cache.Get(typeof(AlsoCached));
            cache.Get(typeof(Cached));

            Assert.That(cache.CompiledCount, Is.EqualTo(2));
        }

        [Test]
        public void Should_not_hold_the_type_of_an_entry_it_stores()
        {
            // What this asserts is narrow and deliberately so: this table does not hold its own key. It
            // is not a statement that a module becomes collectible — asking a resolver for a type's
            // formatter and compiling a delegate over it both root that type before this cache stores
            // anything, which is why nothing is compiled here. The operational bound is the finite
            // admitted contract set plus process restart, and the weak key only means this table can
            // never be the thing that holds a type.
            var cache = new ConcreteFormatterCache<ICached>(_ => Nothing);

            var stored = StoreWithoutCompiling(cache);

            Collect(stored);

            Assert.That(stored.IsAlive, Is.False,
                "the table holds its key strongly, so it would keep a runtime generated type and its "
                + "module alive for the life of the process");
        }

        [Test]
        public void Should_release_a_collectible_type_the_cache_never_saw()
        {
            // The control for the case above: without this, a green release assertion could be green
            // because the test never manages to make anything collectible in the first place.
            var untouched = EmitCollectibleImplementation();

            Collect(untouched);

            Assert.That(untouched.IsAlive, Is.False);
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
        public void Should_write_through_the_formatter_of_the_runtime_type()
        {
            // The value arrives typed as the interface and is written by the formatter of whatever it
            // actually is. That cast used to be a reinterpretation of the compiled delegate that the
            // runtime never checked; it happens inside the compiled body now.
            var round = RoundTrip(new AlsoCached { Id = 3, Name = "other implementation" });

            Assert.That(round.Id, Is.EqualTo(3));
        }

        [Test]
        public void Should_stay_correct_when_many_threads_serialize_at_once()
        {
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

        static void Collect(WeakReference reference)
        {
            for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Kept out of the test body on purpose: the type may not still be reachable from a caller's
        /// stack frame when the collection runs, or the assertion would be about the frame rather than
        /// about what is being measured.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference StoreWithoutCompiling(ConcreteFormatterCache<ICached> cache)
        {
            var collectible = EmitCollectibleImplementationType();

            cache.Get(collectible);

            return new WeakReference(collectible);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference EmitCollectibleImplementation()
        {
            return new WeakReference(EmitCollectibleImplementationType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Type EmitCollectibleImplementationType()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CollectibleContracts"),
                AssemblyBuilderAccess.RunAndCollect);

            var builder = assembly.DefineDynamicModule("main")
                .DefineType("CollectibleCached", TypeAttributes.Public, typeof(object), new[] { typeof(ICached) });

            DefineGetOnlyProperty(builder, "Id", typeof(int));
            DefineGetOnlyProperty(builder, "Name", typeof(string));

            return builder.CreateType();
        }

        static void DefineGetOnlyProperty(TypeBuilder builder, string name, Type propertyType)
        {
            var field = builder.DefineField("_" + name, propertyType, FieldAttributes.Private);

            var getter = builder.DefineMethod("get_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
                propertyType, Type.EmptyTypes);

            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            builder.DefineProperty(name, PropertyAttributes.None, propertyType, null).SetGetMethod(getter);
            builder.DefineMethodOverride(getter, typeof(ICached).GetMethod("get_" + name));
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


        static readonly ConcreteFormatterAccess<ICached> Nothing =
            new ConcreteFormatterAccess<ICached>(null, null, null);


        public interface ICached
        {
            int Id { get; }
            string Name { get; }
        }


        public class Cached :
            ICached
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }


        public class AlsoCached :
            ICached
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
