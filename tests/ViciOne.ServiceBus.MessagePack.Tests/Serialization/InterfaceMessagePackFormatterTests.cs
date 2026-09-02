using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Resolvers;
using ViciOne.ServiceBus.Serialization.MessagePackFormatters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class InterfaceMessagePackFormatterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "proxy-roundtrip")]
    public void InterfaceContract_RoundTripsAllAccessorShapes()
    {
        PersonContract source = new PersonMessage
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
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "same-type-compiled-once")]
    public void SameConcreteType_IsCompiledOnce()
    {
        var cache = new ConcreteFormatterCache<ICached>();

        for (var index = 0; index < 100; index++)
        {
            _ = cache.Get(typeof(Cached));
        }

        Assert.Equal(1, cache.CompiledCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "concurrent-cold-cache")]
    public async Task ConcurrentColdCache_CompilesOneSharedEntry()
    {
        var cache = new ConcreteFormatterCache<ICached>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ConcreteFormatterAccess<ICached>>[] calls = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(async () =>
            {
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                return cache.Get(typeof(Cached));
            }, TestContext.Current.CancellationToken))
            .ToArray();

        release.SetResult();
        ConcreteFormatterAccess<ICached>[] entries = await Task.WhenAll(calls);

        Assert.Equal(1, cache.CompiledCount);
        Assert.All(entries, entry => Assert.Same(entries[0], entry));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "one-entry-per-concrete-type")]
    public void DifferentConcreteTypes_OwnDifferentCompiledEntries()
    {
        var cache = new ConcreteFormatterCache<ICached>();

        _ = cache.Get(typeof(Cached));
        _ = cache.Get(typeof(AlsoCached));
        _ = cache.Get(typeof(Cached));

        Assert.Equal(2, cache.CompiledCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORMATTER-CACHE", "weak-key")]
    public void Cache_DoesNotKeepItsOwnTypeKeyAlive()
    {
        var cache = new ConcreteFormatterCache<ICached>(_ => EmptyAccess);
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
    private static WeakReference StoreWithoutCompiling(ConcreteFormatterCache<ICached> cache)
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

    private static readonly ConcreteFormatterAccess<ICached> EmptyAccess = new(null!, null!, null!);

    public interface ICached
    {
        int Id { get; }

        string Name { get; }
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
}
