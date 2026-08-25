namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using global::ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class TypeRelationshipExtensionsTests
{
    [Theory]
    [InlineData(typeof(DirectImplementation), typeof(IMarker<int>))]
    [InlineData(typeof(DirectImplementation), typeof(IMarker<>))]
    [InlineData(typeof(InheritedImplementation), typeof(IMarker<int>))]
    [InlineData(typeof(InheritedImplementation), typeof(IMarker<>))]
    [InlineData(typeof(DirectImplementation), typeof(INonGeneric))]
    [InlineData(typeof(DerivedImplementation), typeof(INonGeneric))]
    [InlineData(typeof(IMarker<>), typeof(IMarker<>))]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "implemented-interface")]
    public void ImplementsInterface_MatchesClosedOpenAndInheritedInterfaces(Type type, Type interfaceType)
    {
        Assert.True(type.ImplementsInterface(interfaceType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "implemented-interface-generic-overload")]
    public void ImplementsInterfaceGenericOverload_MatchesDirectAndInheritedInterfaces()
    {
        Assert.True(typeof(DirectImplementation).ImplementsInterface<IMarker<int>>());
        Assert.True(typeof(InheritedImplementation).ImplementsInterface<IMarker<int>>());
        Assert.True(typeof(DirectImplementation).ImplementsInterface<INonGeneric>());
        Assert.True(typeof(DerivedImplementation).ImplementsInterface<INonGeneric>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "missing-interface")]
    public void ImplementsInterface_ReturnsFalseForAnUnimplementedInterface()
    {
        Assert.False(typeof(DirectImplementation).ImplementsInterface<IDisposable>());
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(List<>))]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "invalid-interface-shape")]
    public void ImplementsInterface_RejectsTypesThatAreNotInterfaces(Type invalidInterfaceType)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => typeof(DirectImplementation).ImplementsInterface(invalidInterfaceType));

        Assert.Equal("interfaceType", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "partially-open-interface")]
    public void ImplementsInterface_RejectsAPartiallyOpenInterface()
    {
        Type genericParameter = typeof(GenericHolder<>).GetGenericArguments()[0];
        Type partiallyOpenInterface = typeof(IMarker<>).MakeGenericType(genericParameter);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => typeof(DirectImplementation).ImplementsInterface(partiallyOpenInterface));

        Assert.Equal("interfaceType", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "null-interface-inputs")]
    public void ImplementsInterface_RejectsMissingInputs()
    {
        Type? missingType = null;

        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => missingType!.ImplementsInterface<INonGeneric>()).ParamName);
        Assert.Equal("interfaceType",
            Assert.Throws<ArgumentNullException>(() => typeof(DirectImplementation).ImplementsInterface(null!)).ParamName);
    }

    [Theory]
    [InlineData(typeof(DirectImplementation), typeof(IMarker<>), true)]
    [InlineData(typeof(IMarker<int>), typeof(IMarker<>), true)]
    [InlineData(typeof(InheritedImplementation), typeof(GenericBase<>), true)]
    [InlineData(typeof(GenericBase<>), typeof(IMarker<>), false)]
    [InlineData(typeof(IMarker<>), typeof(IMarker<>), false)]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "closed-generic-match")]
    public void ClosesGenericType_RequiresAFullyClosedMatch(Type type, Type genericTypeDefinition, bool expected)
    {
        Assert.Equal(expected, type.ClosesGenericType(genericTypeDefinition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "interface-closing-arguments")]
    public void GetSingleClosedGenericArguments_ResolvesDirectNestedAndInheritedInterfaces()
    {
        Assert.True(typeof(DirectImplementation).TryGetSingleClosedGenericType(typeof(IMarker<>), out Type? closedType));
        Assert.Equal(typeof(IMarker<int>), closedType);
        Assert.Equal([typeof(int)], typeof(DirectImplementation).GetSingleClosedGenericArguments(typeof(IMarker<>)));
        Assert.Equal(typeof(int), typeof(DirectImplementation).GetSingleClosedGenericArgument(typeof(IMarker<>)));
        Assert.Equal([typeof(int)], typeof(InheritedImplementation).GetSingleClosedGenericArguments(typeof(IMarker<>)));
        Assert.Equal([typeof(DirectImplementation), typeof(int)],
            typeof(ConstrainedImplementation).GetSingleClosedGenericArguments(typeof(IConstrained<,>)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "class-closing-arguments")]
    public void GetSingleClosedGenericArguments_ResolvesDirectAndInheritedClasses()
    {
        Assert.Equal([typeof(string)], typeof(List<string>).GetSingleClosedGenericArguments(typeof(List<>)));
        Assert.Equal([typeof(string)], typeof(DeepList).GetSingleClosedGenericArguments(typeof(List<>)));
        Assert.Equal([typeof(int), typeof(string)],
            typeof(DeepDictionary).GetSingleClosedGenericArguments(typeof(Dictionary<,>)));
        Assert.Equal([typeof(int)], typeof(InheritedImplementation).GetSingleClosedGenericArguments(typeof(GenericBase<>)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "multiple-generic-matches")]
    public void GetClosedGenericTypes_ReturnsEveryMatchInStableOrder()
    {
        IReadOnlyList<Type> matches = typeof(AmbiguousImplementation).GetClosedGenericTypes(typeof(IMarker<>));

        Assert.Equal([typeof(IMarker<int>), typeof(IMarker<string>)], matches);
        var mutableView = Assert.IsAssignableFrom<IList<Type>>(matches);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView[0] = typeof(IMarker<decimal>));
        Assert.Equal([typeof(IMarker<int>), typeof(IMarker<string>)],
            typeof(AmbiguousImplementation).GetClosedGenericTypes(typeof(IMarker<>)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "ambiguous-single-match")]
    public void TryGetSingleClosedGenericType_RejectsAmbiguousMatches()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => typeof(AmbiguousImplementation).TryGetSingleClosedGenericType(typeof(IMarker<>), out _));

        Assert.Contains("Int32", exception.Message, StringComparison.Ordinal);
        Assert.Contains("String", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "missing-single-match")]
    public void SingleClosedGenericAccessors_DistinguishAMissingMatch()
    {
        Assert.Empty(typeof(DirectImplementation).GetClosedGenericTypes(typeof(IReadOnlyList<>)));
        Assert.False(typeof(DirectImplementation).TryGetSingleClosedGenericType(typeof(IReadOnlyList<>), out Type? closedType));
        Assert.Null(closedType);
        Assert.False(typeof(DirectImplementation).TryGetSingleClosedGenericArguments(typeof(IReadOnlyList<>), out Type[] arguments));
        Assert.Empty(arguments);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => typeof(DirectImplementation).GetSingleClosedGenericArguments(typeof(IReadOnlyList<>)));
        Assert.Equal("type", exception.ParamName);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(List<int>))]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "generic-definition-required")]
    public void GenericRelationshipOperations_RequireAGenericTypeDefinition(Type invalidDefinition)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => typeof(DirectImplementation).ClosesGenericType(invalidDefinition));

        Assert.Equal("genericTypeDefinition", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "null-generic-inputs")]
    public void GenericRelationshipOperations_RejectMissingInputs()
    {
        Type? missingType = null;

        Assert.Equal("type",
            Assert.Throws<ArgumentNullException>(() => missingType!.ClosesGenericType(typeof(IMarker<>))).ParamName);
        Assert.Equal("genericTypeDefinition",
            Assert.Throws<ArgumentNullException>(() => typeof(DirectImplementation).GetClosedGenericTypes(null!)).ParamName);
    }

    [Theory]
    [InlineData(typeof(Task<int>), typeof(int), true)]
    [InlineData(typeof(DerivedTask), typeof(int), true)]
    [InlineData(typeof(Task), null, false)]
    [InlineData(typeof(Task<>), null, false)]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "task-result-type")]
    public void TryGetTaskResultType_ReturnsOnlyAClosedTaskResult(Type type, Type? expectedResultType, bool expected)
    {
        bool result = type.TryGetTaskResultType(out Type? resultType);

        Assert.Equal(expected, result);
        Assert.Equal(expectedResultType, resultType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-RELATIONSHIPS", "single-generic-argument-required")]
    public void GetSingleClosedGenericArgument_RejectsAMultiArgumentDefinition()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => typeof(Dictionary<int, string>).GetSingleClosedGenericArgument(typeof(Dictionary<,>)));

        Assert.Contains("2 generic arguments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TYPE-CACHE-LIFETIME", "collectible-type")]
    public void RelationshipAndNameCaches_DoNotRootCollectibleTypes()
    {
        WeakReference typeReference = CacheACollectibleType();

        for (var attempt = 0; typeReference.IsAlive && attempt < 10; attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        Assert.False(typeReference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static WeakReference CacheACollectibleType()
    {
        var assemblyName = new AssemblyName("ViciOne.ServiceBus.Tests.CollectibleTypeRelationshipProbe");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule(assemblyName.Name!);
        TypeBuilder builder = module.DefineType(
            "CollectibleMarker",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        builder.AddInterfaceImplementation(typeof(ICollectibleMarker<int>));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        Type implementation = builder.CreateType()!;

        Assert.True(implementation.ClosesGenericType(typeof(ICollectibleMarker<>)));
        Assert.Equal([typeof(ICollectibleMarker<int>)], implementation.GetClosedGenericTypes(typeof(ICollectibleMarker<>)));
        Assert.Contains("CollectibleMarker", TypeCache.GetShortName(implementation), StringComparison.Ordinal);

        return new WeakReference(implementation);
    }

    private interface INonGeneric;

    private interface IMarker<T>;

    private interface IConstrained<TContract, TValue>
        where TContract : IMarker<TValue>;

    public interface ICollectibleMarker<T>;

    private class DirectImplementation : IMarker<int>, INonGeneric;

    private sealed class DerivedImplementation : DirectImplementation;

    private class GenericBase<T> : IMarker<T>;

    private sealed class InheritedImplementation : GenericBase<int>;

    private sealed class AmbiguousImplementation : IMarker<string>, IMarker<int>;

    private sealed class ConstrainedImplementation : IConstrained<DirectImplementation, int>;

    private sealed class DeepList : List<string>;

    private sealed class DeepDictionary : Dictionary<int, string>;

    private sealed class GenericHolder<T>;

    private abstract class DerivedTask : Task<int>
    {
        protected DerivedTask()
            : base(static () => 1)
        {
        }
    }
}
