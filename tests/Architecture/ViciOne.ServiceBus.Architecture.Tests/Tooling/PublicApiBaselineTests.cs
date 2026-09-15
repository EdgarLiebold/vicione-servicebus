using ViciOne.ServiceBus.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;

public sealed class PublicApiBaselineTests
{
    private const string Prefix = "ViciOne.ServiceBus.Architecture.Tests.Tooling.PublicApiBaselineTests.";

    [Theory]
    [InlineData(typeof(Outer<>), Prefix + "Outer<TOuter>")]
    [InlineData(typeof(Outer<>.Inner<,>), Prefix + "Outer<TOuter>.Inner<TLeft,TRight>")]
    [InlineData(typeof(Outer<int>.Inner<string, Guid>), Prefix + "Outer<System.Int32>.Inner<System.String,System.Guid>")]
    [InlineData(typeof(Outer<int>.Inner<string>), Prefix + "Outer<System.Int32>.Inner<System.String>")]
    [InlineData(typeof(Outer<>.Plain), Prefix + "Outer<TOuter>.Plain")]
    [InlineData(typeof(Outer<long>.Plain), Prefix + "Outer<System.Int64>.Plain")]
    [InlineData(typeof(Outer<>.Plain.Branch<>), Prefix + "Outer<TOuter>.Plain.Branch<TBranch>")]
    [InlineData(typeof(Outer<long>.Plain.Branch<decimal>), Prefix + "Outer<System.Int64>.Plain.Branch<System.Decimal>")]
    [InlineData(typeof(NonGeneric.GenericChild<>), Prefix + "NonGeneric.GenericChild<TChild>")]
    [InlineData(typeof(NonGeneric.GenericChild<decimal>), Prefix + "NonGeneric.GenericChild<System.Decimal>")]
    [InlineData(typeof(Outer<>.Inner<,>.Leaf<>), Prefix + "Outer<TOuter>.Inner<TLeft,TRight>.Leaf<TLeaf>")]
    [InlineData(typeof(Outer<int>.Inner<string, Guid>.Leaf<decimal>), Prefix + "Outer<System.Int32>.Inner<System.String,System.Guid>.Leaf<System.Decimal>")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-preserves-each-declaring-segment-and-own-arity")]
    public void FormatType_PreservesDeclaringSegmentsAndTheirOwnArguments(Type type, string expected)
        => Assert.Equal(expected, PublicApiBaseline.FormatType(type));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-distinguishes-nested-child-and-closed-parent")]
    public void FormatType_DistinguishesChildNamesAndActualClosedParentArguments()
    {
        string plain = PublicApiBaseline.FormatType(typeof(Outer<int>.Plain));
        string sibling = PublicApiBaseline.FormatType(typeof(Outer<int>.Sibling));
        string differentParent = PublicApiBaseline.FormatType(typeof(Outer<long>.Plain));

        Assert.Equal(Prefix + "Outer<System.Int32>.Plain", plain);
        Assert.Equal(Prefix + "Outer<System.Int32>.Sibling", sibling);
        Assert.Equal(Prefix + "Outer<System.Int64>.Plain", differentParent);
        Assert.NotEqual(plain, sibling);
        Assert.NotEqual(plain, differentParent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-recursively-formats-nested-generic-arguments")]
    public void FormatType_PreservesNestedTypesInsideGenericArguments()
        => Assert.Equal(
            "System.Collections.Generic.Dictionary<System.String," + Prefix + "Outer<System.Int32>.Inner<System.Guid,System.Decimal>>",
            PublicApiBaseline.FormatType(typeof(Dictionary<string, Outer<int>.Inner<Guid, decimal>>)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-preserves-generic-parameter-names")]
    public void FormatType_PreservesEachGenericParameterName()
        => Assert.Equal(
            ["TOuter", "TLeft", "TRight"],
            typeof(Outer<>.Inner<,>).GetGenericArguments().Select(PublicApiBaseline.FormatType));

    [Theory]
    [InlineData(typeof(int), "System.Int32")]
    [InlineData(typeof(int[]), "System.Int32[]")]
    [InlineData(typeof(int[,]), "System.Int32[,]")]
    [InlineData(typeof(int[,,]), "System.Int32[,,]")]
    [InlineData(typeof(int[][]), "System.Int32[][]")]
    [InlineData(typeof(NonGeneric), Prefix + "NonGeneric")]
    [InlineData(typeof(Outer<int>.Plain[,]), Prefix + "Outer<System.Int32>.Plain[,]")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-preserves-ordinary-names-and-array-shapes")]
    public void FormatType_PreservesOrdinaryNamesAndArrayShapes(Type type, string expected)
        => Assert.Equal(expected, PublicApiBaseline.FormatType(type));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-distinguishes-vector-and-non-vector-rank-one-arrays")]
    public void FormatType_DistinguishesVectorAndNonVectorRankOneArrays()
    {
        Assert.Equal("System.Int32[]", PublicApiBaseline.FormatType(typeof(int).MakeArrayType()));
        Assert.Equal("System.Int32[*]", PublicApiBaseline.FormatType(typeof(int).MakeArrayType(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "type-identity-preserves-reference-and-pointer-modifiers")]
    public void FormatType_PreservesReferenceAndPointerModifiers()
    {
        Assert.Equal("System.Int32&", PublicApiBaseline.FormatType(typeof(int).MakeByRefType()));
        Assert.Equal("System.Int32*", PublicApiBaseline.FormatType(typeof(int).MakePointerType()));
        Assert.Equal(
            Prefix + "Outer<System.Int64>.Plain&",
            PublicApiBaseline.FormatType(typeof(Outer<long>.Plain).MakeByRefType()));
    }

    private sealed class Outer<TOuter>
    {
        public sealed class Plain
        {
            public sealed class Branch<TBranch>;
        }
        public sealed class Sibling;
        public sealed class Inner<TSingle>;
        public sealed class Inner<TLeft, TRight>
        {
            public sealed class Leaf<TLeaf>;
        }
    }

    private sealed class NonGeneric
    {
        public sealed class GenericChild<TChild>;
    }
}
