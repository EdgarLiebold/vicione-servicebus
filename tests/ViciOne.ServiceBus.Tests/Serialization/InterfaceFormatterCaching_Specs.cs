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
    using MessagePack.Formatters;
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
            // The old cache was a static dictionary with strong keys, justified by the claim that
            // concrete types are a closed set. That is not a bound: a module can introduce a contract
            // type at runtime, and a strong key would then hold that type, and the assembly behind it,
            // for the life of the process. The entries are held against weak keys instead.
            //
            // Nothing is compiled here, because compiling for a runtime generated type roots it in the
            // runtime's own tables; the case below measures that. What is asserted here is the one
            // thing this table decides on its own: whether it lets go of a key nobody else holds.
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
        public void Should_record_that_the_serializer_itself_retains_a_runtime_generated_type()
        {
            // Measured, and the reason the case above deliberately compiles nothing: asking the resolver
            // for the formatter of a runtime generated type is already enough to root that type, and so
            // is compiling any delegate over it. Both happen before this cache stores anything, so no
            // storage design in this module can deliver release while contractless formatters are
            // generated at runtime.
            //
            // This is a characterisation of the dependency, not of our code. If a later MessagePack
            // version stops rooting the type, this turns red and the entry cache should be revisited
            // together with the versioned formatter work.
            var resolved = ResolveFormatterFor(EmitCollectibleImplementationType());

            Collect(resolved);

            Assert.That(resolved.IsAlive, Is.True,
                "the serializer no longer retains a runtime generated type; the cache can now be held to "
                + "the stronger release statement");
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
        static WeakReference ResolveFormatterFor(Type collectible)
        {
            typeof(IFormatterResolver)
                .GetMethod(nameof(IFormatterResolver.GetFormatter))
                .MakeGenericMethod(collectible)
                .Invoke(MessagePack.Resolvers.ContractlessStandardResolver.Instance, null);

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
