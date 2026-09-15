using ViciOne.ServiceBus.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;

public sealed class PublicApiGenericContractTests
{
    private const string Prefix = "ViciOne.ServiceBus.Architecture.Tests.Tooling.PublicApiGenericContractTests.";

    [Theory]
    [InlineData(typeof(Unconstrained<>), "GENERIC T{flags=None;constraints=[];nullable=[2];unmanaged=false}")]
    [InlineData(typeof(RequiredReference<>), "GENERIC T{flags=ReferenceTypeConstraint;constraints=[];nullable=[1];unmanaged=false}")]
    [InlineData(typeof(NullableReference<>), "GENERIC T{flags=ReferenceTypeConstraint;constraints=[];nullable=[2];unmanaged=false}")]
    [InlineData(typeof(RequiredValue<>), "GENERIC T{flags=NotNullableValueTypeConstraint&DefaultConstructorConstraint;constraints=[System.ValueType];nullable=[0];unmanaged=false}")]
    [InlineData(typeof(RequiredConstructor<>), "GENERIC T{flags=DefaultConstructorConstraint;constraints=[];nullable=[2];unmanaged=false}")]
    [InlineData(typeof(RequiredUnmanaged<>), "GENERIC T{flags=NotNullableValueTypeConstraint&DefaultConstructorConstraint;constraints=[System.ValueType];nullable=[0];unmanaged=true}")]
    [InlineData(typeof(RequiredNotNull<>), "GENERIC T{flags=None;constraints=[];nullable=[1];unmanaged=false}")]
    [InlineData(typeof(ByRefLikeAllowed<>), "GENERIC T{flags=AllowByRefLike;constraints=[];nullable=[2];unmanaged=false}")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-preserve-required-constraints-and-annotations")]
    public void TypeGenericContracts_PreserveRequiredConstraintsAndAnnotations(Type type, string expected)
        => Assert.Equal(expected, Assert.Single(GenericRecords(type)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-preserve-both-variance-directions")]
    public void TypeGenericContracts_PreserveBothVarianceDirections()
        => Assert.Equal(
            [
                "GENERIC TInput{flags=Contravariant&ReferenceTypeConstraint;constraints=[];nullable=[1];unmanaged=false}",
                "GENERIC TOutput{flags=Covariant&ReferenceTypeConstraint;constraints=[];nullable=[2];unmanaged=false}",
            ],
            GenericRecords(typeof(IVariant<,>)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-preserve-all-explicit-constraints-in-ordinal-order")]
    public void TypeGenericContracts_PreserveAllExplicitConstraintsInOrdinalOrder()
    {
        string record = Assert.Single(GenericRecords(typeof(ExplicitConstraints<>)));

        Assert.Contains("T{flags=None;constraints=[System.IAsyncDisposable&System.IDisposable&System.IO.Stream];", record, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-preserve-dependent-parameter-constraints")]
    public void TypeGenericContracts_PreserveDependentParameterConstraints()
    {
        string[] records = GenericRecords(typeof(Dependent<,>));

        Assert.Equal(2, records.Length);
        Assert.Equal("GENERIC TAnchor{flags=ReferenceTypeConstraint;constraints=[];nullable=[1];unmanaged=false}", records[0]);
        Assert.Contains("TDependent{flags=None;constraints=[TAnchor];", records[1], StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-preserve-constructed-nested-constraint-identities")]
    public void TypeGenericContracts_PreserveConstructedNestedConstraintIdentities()
        => Assert.Contains(
            "constraints=[System.Collections.Generic.IReadOnlyCollection<" + Prefix + "Outer<System.String>.Inner<System.Int32>>];",
            Assert.Single(GenericRecords(typeof(ConstructedConstraint<>))),
            StringComparison.Ordinal);

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-type-contracts-do-not-duplicate-inherited-parameters")]
    public void TypeGenericContracts_DoNotDuplicateInheritedParameters()
    {
        Assert.Equal(
            ["GENERIC TInner{flags=NotNullableValueTypeConstraint&DefaultConstructorConstraint;constraints=[System.ValueType];nullable=[0];unmanaged=false}"],
            GenericRecords(typeof(Outer<>.Inner<>)));
        Assert.Empty(GenericRecords(typeof(Outer<>.Plain)));
        Assert.Empty(GenericRecords(typeof(Outer<string>.Inner<int>)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-method-contracts-bind-constraints-to-their-own-declarations")]
    public void MethodGenericContracts_BindConstraintsToTheirOwnDeclarations()
        => Assert.Equal(
            [
                "METHOD public static System.Int32 Ordinary(System.Int32 value)",
                "METHOD public static T MaybeReference<T>(T value [nullability={read=Nullable;write=Nullable}]) [generic=T{flags=ReferenceTypeConstraint;constraints=[];nullable=[2];unmanaged=false}] [return-nullability={read=Nullable;write=Nullable}]",
                "METHOD public static T Pair<T,TValue>(T value, TValue other) [generic=T{flags=ReferenceTypeConstraint;constraints=[];nullable=[1];unmanaged=false},TValue{flags=NotNullableValueTypeConstraint&DefaultConstructorConstraint;constraints=[System.ValueType];nullable=[0];unmanaged=false}]",
                "METHOD public static T Plain<T>(T value [nullability={read=Nullable;write=Nullable}]) [generic=T{flags=None;constraints=[];nullable=[2];unmanaged=false}] [return-nullability={read=Nullable;write=Nullable}]",
                "METHOD public static T Reference<T>(T value) [generic=T{flags=ReferenceTypeConstraint&DefaultConstructorConstraint;constraints=[];nullable=[1];unmanaged=false}]",
            ],
            PublicApiBaseline.FormatMembers(typeof(MethodContracts))
                .Where(row => row.StartsWith("METHOD ", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "generic-method-contracts-do-not-misclassify-ordinary-members")]
    public void MethodGenericContracts_DoNotMisclassifyOrdinaryMembers()
    {
        Assert.Empty(GenericRecords(typeof(MethodContracts)));
        Assert.Equal(
            "METHOD public static System.Int32 Ordinary(System.Int32 value)",
            Assert.Single(PublicApiBaseline.FormatMembers(typeof(MethodContracts)), row => row.Contains(" Ordinary(", StringComparison.Ordinal)));
    }

    private static string[] GenericRecords(Type type) => PublicApiBaseline.FormatMembers(type)
        .Where(row => row.StartsWith("GENERIC ", StringComparison.Ordinal))
        .ToArray();

    private sealed class Unconstrained<T>;
    private sealed class RequiredReference<T> where T : class;
    private sealed class NullableReference<T> where T : class?;
    private sealed class RequiredValue<T> where T : struct;
    private sealed class RequiredConstructor<T> where T : new();
    private sealed class RequiredUnmanaged<T> where T : unmanaged;
    private sealed class RequiredNotNull<T> where T : notnull;
    private sealed class ByRefLikeAllowed<T> where T : allows ref struct;

    private interface IVariant<in TInput, out TOutput>
        where TInput : class
        where TOutput : class?;

    private sealed class ExplicitConstraints<T> where T : Stream, IDisposable, IAsyncDisposable;
    private sealed class Dependent<TAnchor, TDependent> where TAnchor : class where TDependent : TAnchor;
    private sealed class ConstructedConstraint<T> where T : IReadOnlyCollection<Outer<string>.Inner<int>>;

    private sealed class Outer<TOuter> where TOuter : class
    {
        public sealed class Inner<TInner> where TInner : struct;
        public sealed class Plain;
    }

    private static class MethodContracts
    {
        public static int Ordinary(int value) => value;
        public static T MaybeReference<T>(T value) where T : class? => value;
        public static T Pair<T, TValue>(T value, TValue other) where T : class where TValue : struct => value;
        public static T Plain<T>(T value) => value;
        public static T Reference<T>(T value) where T : class, new() => value;
    }
}
