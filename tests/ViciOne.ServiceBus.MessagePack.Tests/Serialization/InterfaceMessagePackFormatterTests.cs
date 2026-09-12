using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.MessagePack.Serialization.Formatters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class InterfaceMessagePackFormatterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "proxy-roundtrip")]
    public void InterfaceContract_RoundTripsAllAccessorShapes()
    {
        IPersonContract source = new PersonMessage
        {
            Id = 27,
            Name = "Frank",
            Contact = new ContactMessage { Email = "frank@example.test" },
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(27, result.Id);
        Assert.Equal("Frank", result.Name);
        Assert.Equal("frank@example.test", result.Contact.Email);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "null-roundtrip")]
    public void InterfaceContract_RoundTripsNull()
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);
        var formatter = new InterfaceMessagePackFormatter<IPersonContract>();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        formatter.Serialize(ref writer, null!, options);
        writer.Flush();
        var reader = new MessagePackReader(buffer.WrittenMemory);
        IPersonContract result = formatter.Deserialize(ref reader, options);

        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "declared-concrete-null-roundtrip")]
    public void DeclaredConcreteMapping_RoundTripsNull()
    {
        byte[] bytes = SerializeMapped(null!);

        IMappedContract result = DeserializeMapped(bytes);

        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "declared-concrete-runtime-type-guard")]
    public void DeclaredConcreteMapping_RejectsAnotherImplementation()
    {
        var value = new AlternativeMappedContract { Value = "unsupported" };

        MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
            () => SerializeMapped(value));

        Assert.Contains(typeof(AlternativeMappedContract).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(MappedContract).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "same-type-compiled-once")]
    public void SameConcreteType_IsCompiledOnce()
    {
        var compilationCount = 0;
        var cache = new ConcreteFormatterInvokerCache<ICached>(_ =>
        {
            Interlocked.Increment(ref compilationCount);
            return EmptyInvoker;
        });

        for (var index = 0; index < 100; index++)
        {
            _ = cache.Get(typeof(Cached));
        }

        Assert.Equal(1, compilationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "complete-concrete-type-boundary")]
    public void Cache_RejectsMissingFactoriesAndNonConcreteContractImplementations()
    {
        Assert.Equal("build", Assert.Throws<ArgumentNullException>(() =>
            new ConcreteFormatterInvokerCache<ICached>(null!)).ParamName);

        var cache = new ConcreteFormatterInvokerCache<ICached>(_ => FunctionalInvoker);

        Assert.Equal("concreteType", Assert.Throws<ArgumentNullException>(() => cache.Get(null!)).ParamName);
        Assert.Equal("concreteType", Assert.Throws<ArgumentException>(() => cache.Get(typeof(ICached))).ParamName);
        Assert.Equal("concreteType", Assert.Throws<ArgumentException>(() => cache.Get(typeof(AbstractCached))).ParamName);
        Assert.Equal("concreteType", Assert.Throws<ArgumentException>(() => cache.Get(typeof(string))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "complete-invoker-boundary")]
    public void Invoker_RequiresEveryFormatterOperation()
    {
        Assert.Equal("getFormatter", Assert.Throws<ArgumentNullException>(() =>
            new ConcreteFormatterInvoker<ICached>(null!, SerializeCached, DeserializeCached)).ParamName);
        Assert.Equal("serialize", Assert.Throws<ArgumentNullException>(() =>
            new ConcreteFormatterInvoker<ICached>(GetFormatter, null!, DeserializeCached)).ParamName);
        Assert.Equal("deserialize", Assert.Throws<ArgumentNullException>(() =>
            new ConcreteFormatterInvoker<ICached>(GetFormatter, SerializeCached, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "concurrent-cold-cache")]
    public async Task ConcurrentColdCache_CompilesOneSharedEntryAsync()
    {
        var compilationCount = 0;
        var cache = new ConcreteFormatterInvokerCache<ICached>(_ =>
        {
            Interlocked.Increment(ref compilationCount);
            return EmptyInvoker;
        });
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ConcreteFormatterInvoker<ICached>>[] calls =
        [..
            Enumerable.Range(0, 16)
            .Select(_ => Task.Run(async () =>
            {
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                return cache.Get(typeof(Cached));
            }, TestContext.Current.CancellationToken)),
        ];

        release.SetResult();
        ConcreteFormatterInvoker<ICached>[] entries = await Task.WhenAll(calls);

        Assert.Equal(1, compilationCount);
        Assert.All(entries, entry => Assert.Same(entries[0], entry));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "one-entry-per-concrete-type")]
    public void DifferentConcreteTypes_OwnDifferentCompiledEntries()
    {
        var compilationCount = 0;
        var cache = new ConcreteFormatterInvokerCache<ICached>(_ =>
        {
            Interlocked.Increment(ref compilationCount);
            return EmptyInvoker;
        });

        _ = cache.Get(typeof(Cached));
        _ = cache.Get(typeof(AlsoCached));
        _ = cache.Get(typeof(Cached));

        Assert.Equal(2, compilationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "failed-compilation-is-cached")]
    public void FailedCompilation_IsCachedForTheConcreteType()
    {
        var compilationCount = 0;
        var cache = new ConcreteFormatterInvokerCache<ICached>(_ =>
        {
            Interlocked.Increment(ref compilationCount);
            throw new InvalidOperationException("Rejected test formatter.");
        });

        var first = Assert.Throws<InvalidOperationException>(() => cache.Get(typeof(Cached)));
        var second = Assert.Throws<InvalidOperationException>(() => cache.Get(typeof(Cached)));

        Assert.Equal("Rejected test formatter.", first.Message);
        Assert.Same(first, second);
        Assert.Equal(1, compilationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "weak-key")]
    public void Cache_DoesNotKeepItsOwnTypeKeyAlive()
    {
        var cache = new ConcreteFormatterInvokerCache<ICached>(_ => EmptyInvoker);
        var reference = StoreWithoutCompiling(cache);

        Collect(reference);

        Assert.False(reference.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "collectibility-control")]
    public void CollectibilityControl_ReleasesAnUncachedType()
    {
        var reference = EmitCollectibleImplementation();

        Collect(reference);

        Assert.False(reference.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "runtime-concrete-formatter")]
    public void RuntimeConcreteFormatter_PreservesBothImplementations()
    {
        ICached first = RoundTrip(new Cached { Id = 27, Name = "first" });
        ICached second = RoundTrip(new AlsoCached { Id = 3, Name = "second" });

        Assert.Equal(27, first.Id);
        Assert.Equal("first", first.Name);
        Assert.Equal(3, second.Id);
        Assert.Equal("second", second.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "parallel-roundtrip")]
    public void ParallelRoundTrips_PreserveEveryValue()
    {
        ICached[] results = Enumerable.Range(0, 64)
            .AsParallel()
            .WithDegreeOfParallelism(8)
            .Select(index => RoundTrip(new Cached { Id = index, Name = "parallel" }))
            .ToArray();

        Assert.Equal(Enumerable.Range(0, 64), results.Select(result => result.Id).Order());
        Assert.All(results, result => Assert.Equal("parallel", result.Name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "resolver-concurrent-shared-instance")]
    public async Task Resolver_ConcurrentCallsReturnOneSharedFormatterAsync()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<IMessagePackFormatter<IResolverConcurrencyContract>?>[] calls =
        [..
            Enumerable.Range(0, 16)
            .Select(_ => Task.Run(async () =>
            {
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                return ServiceBusMessagePackFormatterResolver.Instance
                    .GetFormatter<IResolverConcurrencyContract>();
            }, TestContext.Current.CancellationToken)),
        ];

        release.SetResult();
        IMessagePackFormatter<IResolverConcurrencyContract>?[] formatters = await Task.WhenAll(calls);
        IMessagePackFormatter<IResolverConcurrencyContract> formatter = Assert.IsType<
            IMessagePackFormatter<IResolverConcurrencyContract>>(formatters[0], exactMatch: false);

        Assert.All(formatters, candidate => Assert.Same(formatter, candidate));
    }

    private static ICached RoundTrip(ICached source)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);
        var formatter = new InterfaceMessagePackFormatter<ICached>();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Serialize(ref writer, source, options);
        writer.Flush();
        var reader = new MessagePackReader(buffer.WrittenMemory);

        return formatter.Deserialize(ref reader, options);
    }

    private static byte[] SerializeMapped(IMappedContract source)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);
        var formatter = new InterfaceConcreteMapFormatter<IMappedContract, MappedContract>();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Serialize(ref writer, source, options);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static IMappedContract DeserializeMapped(byte[] bytes)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);
        var formatter = new InterfaceConcreteMapFormatter<IMappedContract, MappedContract>();
        var reader = new MessagePackReader(bytes);
        return formatter.Deserialize(ref reader, options);
    }

    private static void Collect(WeakReference reference)
    {
        for (var attempt = 0; attempt < 20 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference StoreWithoutCompiling(ConcreteFormatterInvokerCache<ICached> cache)
    {
        var type = EmitCollectibleImplementationType();
        _ = cache.Get(type);
        return new WeakReference(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference EmitCollectibleImplementation() =>
        new(EmitCollectibleImplementationType());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type EmitCollectibleImplementationType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"CollectibleContracts-{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var builder = assembly.DefineDynamicModule("main")
            .DefineType(
                "CollectibleCached",
                TypeAttributes.Public,
                typeof(object),
                [typeof(ICached)]);

        DefineGetOnlyProperty(builder, nameof(ICached.Id), typeof(int));
        DefineGetOnlyProperty(builder, nameof(ICached.Name), typeof(string));

        return builder.CreateType()!;
    }

    private static void DefineGetOnlyProperty(TypeBuilder builder, string name, Type propertyType)
    {
        var field = builder.DefineField($"_{name}", propertyType, FieldAttributes.Private);
        var getter = builder.DefineMethod(
            $"get_{name}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
            MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
            propertyType,
            Type.EmptyTypes);
        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);
        builder.DefineProperty(name, PropertyAttributes.None, propertyType, null).SetGetMethod(getter);
        builder.DefineMethodOverride(getter, typeof(ICached).GetProperty(name)!.GetMethod!);
    }

    private static readonly ConcreteFormatterInvoker<ICached> FunctionalInvoker = new(
        GetFormatter,
        SerializeCached,
        DeserializeCached);

    private static ConcreteFormatterInvoker<ICached> EmptyInvoker => FunctionalInvoker;

    private static object GetFormatter(IFormatterResolver resolver) =>
        resolver.GetFormatterWithVerify<Cached>();

    private static void SerializeCached(
        object formatter,
        ref MessagePackWriter writer,
        ICached value,
        MessagePackSerializerOptions options)
    {
        ((IMessagePackFormatter<Cached>)formatter).Serialize(ref writer, (Cached)value, options);
    }

    private static ICached DeserializeCached(
        object formatter,
        ref MessagePackReader reader,
        MessagePackSerializerOptions options) =>
        ((IMessagePackFormatter<Cached>)formatter).Deserialize(ref reader, options);

    public interface ICached
    {
        int Id { get; }

        string Name { get; }
    }

    public interface IResolverConcurrencyContract
    {
        string Value { get; }
    }

    public interface IMappedContract
    {
        string Value { get; }
    }

    public sealed class MappedContract : IMappedContract
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class AlternativeMappedContract : IMappedContract
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class Cached : ICached
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class AlsoCached : ICached
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public abstract class AbstractCached : ICached
    {
        public abstract int Id { get; }

        public abstract string Name { get; }
    }
}
