using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class InitializerCacheOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CACHE-OWNERSHIP", "published-convention-snapshot-is-element-immutable")]
    public void ConventionSnapshot_RejectsElementReplacement()
    {
        var original = new NoOpConvention();
        var replacement = new NoOpConvention();
        var registry = new InitializerConventionRegistry([original]);
        IReadOnlyList<IInitializerConvention> snapshot = registry.Conventions;

        if (snapshot is IList<IInitializerConvention> mutableView)
        {
            try
            {
                Assert.Throws<NotSupportedException>(() => mutableView[0] = replacement);
            }
            finally
            {
                if (!ReferenceEquals(snapshot[0], original))
                    mutableView[0] = original;
            }
        }

        Assert.Same(original, snapshot[0]);
        Assert.Same(snapshot, registry.Conventions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CACHE-OWNERSHIP", "convention-cache-does-not-pin-collectible-types")]
    public void ConventionTypeCache_DoesNotRetainCollectibleContractTypes()
    {
        var cache = new ConventionTypeCache(new ObjectCacheFactory(), new NoOpConvention());
        CollectibleReferences references = PopulateConventionCache(cache);

        CollectUntilReleased(references);

        Assert.False(references.Type.IsAlive);
        Assert.False(references.Assembly.IsAlive);
        GC.KeepAlive(cache);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CACHE-OWNERSHIP", "message-cache-does-not-pin-collectible-input-types")]
    public void MessageInitializerCache_DoesNotRetainCollectibleInputTypes()
    {
        CollectibleReferences references = PopulateMessageInitializerCache();

        CollectUntilReleased(references);

        Assert.False(references.Type.IsAlive);
        Assert.False(references.Assembly.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CACHE-OWNERSHIP", "type-converter-cache-does-not-pin-collectible-enums")]
    public void TypeConverterCache_DoesNotRetainCollectibleEnumTypes()
    {
        CollectibleReferences references = PopulateTypeConverterCache();

        CollectUntilReleased(references);

        Assert.False(references.Type.IsAlive);
        Assert.False(references.Assembly.IsAlive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CACHE-OWNERSHIP", "live-key-identity-under-contention")]
    public void Caches_ReuseOneLiveValueUnderContention(int form)
    {
        var values = new ConcurrentBag<object>();
        var factory = new ObjectCacheFactory();
        var conventionCache = new ConventionTypeCache(factory, new NoOpConvention());

        Parallel.For(0, 64, _ =>
        {
            object value = form switch
            {
                0 => ((IConventionTypeCache)conventionCache).GetOrAdd<ConcurrentContract, object>(),
                1 => MessageInitializerCache<ConcurrentMessage>.GetInitializer(typeof(ConcurrentInput)),
                2 => GetConcurrentTypeConverter(),
                _ => throw new ArgumentOutOfRangeException(nameof(form)),
            };
            values.Add(value);
        });

        Assert.Equal(64, values.Count);
        object first = values.First();
        Assert.All(values, value => Assert.Same(first, value));
        Assert.Equal(form == 0 ? 1 : 0, factory.Calls);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static CollectibleReferences PopulateConventionCache(ConventionTypeCache cache)
    {
        (AssemblyBuilder assembly, ModuleBuilder module) = CreateAssembly("Convention");
        Type runtimeType = CreateClass(module, "RuntimeConventionContract");
        MethodInfo getOrAdd = typeof(IConventionTypeCache).GetMethod(nameof(IConventionTypeCache.GetOrAdd))!
            .MakeGenericMethod(runtimeType, typeof(object));

        object first = getOrAdd.Invoke(cache, null)!;
        Assert.Same(first, getOrAdd.Invoke(cache, null));
        return new CollectibleReferences(new WeakReference(runtimeType), new WeakReference(assembly));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static CollectibleReferences PopulateMessageInitializerCache()
    {
        (AssemblyBuilder assembly, ModuleBuilder module) = CreateAssembly("MessageInitializer");
        Type runtimeType = CreateClass(module, "RuntimeInitializerInput");

        Assert.Same(
            MessageInitializerCache<CacheMessage>.GetInitializer(runtimeType),
            MessageInitializerCache<CacheMessage>.GetInitializer(runtimeType));
        return new CollectibleReferences(new WeakReference(runtimeType), new WeakReference(assembly));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static CollectibleReferences PopulateTypeConverterCache()
    {
        (AssemblyBuilder assembly, ModuleBuilder module) = CreateAssembly("TypeConverter");
        EnumBuilder builder = module.DefineEnum("RuntimeConversionState", TypeAttributes.Public, typeof(int));
        builder.DefineLiteral("Unknown", 0);
        builder.DefineLiteral("Ready", 1);
        Type runtimeType = builder.CreateTypeInfo()!.AsType();
        MethodInfo resolve = typeof(TypeConverterCache).GetMethod(nameof(TypeConverterCache.TryGetTypeConverter))!
            .MakeGenericMethod(runtimeType, typeof(string));
        object?[] arguments = [null];

        Assert.Equal(true, resolve.Invoke(null, arguments));
        Assert.NotNull(arguments[0]);
        object first = arguments[0]!;
        object?[] repeatedArguments = [null];
        Assert.Equal(true, resolve.Invoke(null, repeatedArguments));
        Assert.Same(first, repeatedArguments[0]);
        arguments[0] = null;
        repeatedArguments[0] = null;
        return new CollectibleReferences(new WeakReference(runtimeType), new WeakReference(assembly));
    }

    static object GetConcurrentTypeConverter()
    {
        Assert.True(TypeConverterCache.TryGetTypeConverter<ConcurrentState, string>(out var converter));
        return converter;
    }

    static (AssemblyBuilder Assembly, ModuleBuilder Module) CreateAssembly(string purpose)
    {
        var name = new AssemblyName($"ViciOne.Initializer.{purpose}.{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        return (assembly, assembly.DefineDynamicModule(name.Name!));
    }

    static Type CreateClass(ModuleBuilder module, string name)
    {
        TypeBuilder builder = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        return builder.CreateType()!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CollectUntilReleased(CollectibleReferences references)
    {
        for (var attempt = 0; attempt < 20 && (references.Type.IsAlive || references.Assembly.IsAlive); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
    }

    public sealed class CacheMessage
    {
        public string? Value { get; set; }
    }

    public sealed class ConcurrentMessage;

    sealed class ConcurrentInput;

    sealed class ConcurrentContract;

    enum ConcurrentState
    {
        Unknown,
        Ready,
    }

    sealed record CollectibleReferences(WeakReference Type, WeakReference Assembly);

    sealed class ObjectCacheFactory : IConventionTypeCacheFactory
    {
        public int Calls;

        public object Create<T>(IInitializerConvention convention)
            where T : class
        {
            Assert.NotNull(convention);
            Interlocked.Increment(ref Calls);
            return new object();
        }
    }

    sealed class NoOpConvention : IInitializerConvention
    {
        public bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
            where TMessage : class
            where TInput : class
        {
            initializer = null;
            return false;
        }

        public bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
            where TMessage : class
            where TInput : class
        {
            initializer = null;
            return false;
        }

        public bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
            where TMessage : class
            where TInput : class
        {
            initializer = null;
            return false;
        }
    }
}
