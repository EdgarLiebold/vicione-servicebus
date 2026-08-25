namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

using System.Reflection;
using global::ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class TypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(TypeWithTwoIndexers), 4)]
    [InlineData(typeof(DerivedTypeWithIndexer), 4)]
    [RequirementCoverage("REQ-VSB-TYPE-EXTENSIONS", "multiple-indexers")]
    public void GetReadableInstanceProperties_EnumeratesTypesWithDifferentIndexerSignatures(Type type, int expectedCount)
    {
        var properties = type.GetReadableInstanceProperties().ToArray();

        Assert.Equal(expectedCount, properties.Length);
        Assert.Equal(2, properties.Count(property => property.GetIndexParameters().Length == 1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "private-getter-plus-inherited")]
    public void GetReadableStaticProperties_IncludesPrivateGetterAndInheritedProperty()
    {
        var propertyNames = typeof(TypeWithPrivateStaticGetter).GetReadableStaticProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["StaticProp", "ZupMan"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "private-property-plus-inherited")]
    public void GetReadableStaticProperties_IncludesPrivateAndInheritedProperties()
    {
        var propertyNames = typeof(TypeWithPrivateStaticProperty).GetReadableStaticProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["CanWeGetPrivates", "StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "standalone-static-property")]
    public void GetReadableStaticProperties_StandaloneTypeReturnsItsStaticProperty()
    {
        var propertyNames = typeof(StaticPropertyBase).GetReadableStaticProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "inherited-static-property")]
    public void GetReadableStaticProperties_DerivedTypeReturnsInheritedStaticProperty()
    {
        var propertyNames = typeof(DerivedWithoutStaticProperty).GetReadableStaticProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "declared-only-static-property")]
    public void GetDeclaredReadableStaticProperties_ExcludesInheritedStaticProperty()
    {
        var propertyNames = typeof(TypeWithPrivateStaticGetter).GetDeclaredReadableStaticProperties()
            .Select(property => property?.Name
                ?? throw new InvalidOperationException("The reflection helper returned a null property."))
            .ToArray();

        Assert.Equal(["ZupMan"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "class-hierarchy")]
    public void GetReadableInstanceProperties_ReturnsExactBaseAndDerivedProperties()
    {
        var properties = typeof(DerivedProperties).GetReadableInstanceProperties();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal(nameof(BaseProperties.First), property.Name);
                Assert.Equal(typeof(BaseProperties), property.DeclaringType);
            },
            property =>
            {
                Assert.Equal(nameof(BaseProperties.Second), property.Name);
                Assert.Equal(typeof(BaseProperties), property.DeclaringType);
            },
            property =>
            {
                Assert.Equal(nameof(DerivedProperties.Third), property.Name);
                Assert.Equal(typeof(DerivedProperties), property.DeclaringType);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "interface-diamond")]
    public void GetReadableInstanceProperties_TraversesInterfaceDiamondsOnceInStableOrder()
    {
        var properties = typeof(IDiamondProperties).GetReadableInstanceProperties();

        Assert.Equal(
            [nameof(IRootProperties.Root), nameof(ILeftProperties.Left), nameof(IRightProperties.Right), nameof(IDiamondProperties.Derived)],
            properties.Select(property => property.Name));
        Assert.Equal(4, properties.Select(property => property.DeclaringType).Distinct().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "interface-implementation")]
    public void GetReadableInstanceProperties_ReturnsExactConcreteInterfaceImplementationProperties()
    {
        var properties = typeof(DiamondImplementation).GetReadableInstanceProperties();

        Assert.Equal(
            [nameof(DiamondImplementation.Root), nameof(DiamondImplementation.Left), nameof(DiamondImplementation.Right),
                nameof(DiamondImplementation.Derived)],
            properties.Select(property => property.Name));
        Assert.All(properties, property => Assert.Equal(typeof(DiamondImplementation), property.DeclaringType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "derived-interface-hides-base")]
    public void GetReadableInstanceProperties_PlacesAHidingDerivedInterfacePropertyLast()
    {
        var valueProperties = typeof(IDerivedValue).GetReadableInstanceProperties()
            .Where(property => property.Name == nameof(IDerivedValue.Value))
            .ToArray();

        Assert.Equal([typeof(IBaseValue), typeof(IDerivedValue)], valueProperties.Select(property => property.DeclaringType));
        Assert.Equal(typeof(string), valueProperties[^1].PropertyType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "write-only-indexer")]
    public void GetReadableInstanceProperties_DoesNotBorrowAGetterFromAnotherIndexer()
    {
        var indexers = typeof(ReadAndWriteOnlyIndexers).GetReadableInstanceProperties()
            .Where(property => property.Name == "Item")
            .ToArray();

        PropertyInfo indexer = Assert.Single(indexers);
        Assert.Equal(typeof(int), Assert.Single(indexer.GetIndexParameters()).ParameterType);
        Assert.NotNull(indexer.GetGetMethod(nonPublic: true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "write-only-static-property")]
    public void ReadableStaticPropertyOperations_ExcludeWriteOnlyProperties()
    {
        Assert.Equal([nameof(StaticReadWriteShape.Readable)],
            typeof(StaticReadWriteShape).GetReadableStaticProperties().Select(property => property.Name));
        Assert.Equal([nameof(StaticReadWriteShape.Readable)],
            typeof(StaticReadWriteShape).GetDeclaredReadableStaticProperties().Select(property => property.Name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READABLE-PROPERTY-REFLECTION", "null-type")]
    public void ReadablePropertyOperations_RejectAMissingType()
    {
        Type? missingType = null;

        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => missingType!.GetReadableInstanceProperties()).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => missingType!.GetReadableStaticProperties()).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => missingType!.GetDeclaredReadableStaticProperties()).ParamName);
    }

    private class BaseTypeWithIndexer
    {
        public string FirstValue { get; } = "first";

        public string SecondValue { get; } = "second";

        public string this[int index] => index switch
        {
            0 => FirstValue,
            1 => SecondValue,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    private sealed class DerivedTypeWithIndexer : BaseTypeWithIndexer
    {
        public string this[string name] => name switch
        {
            "first" => FirstValue,
            "second" => SecondValue,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    private sealed class TypeWithTwoIndexers
    {
        public string FirstValue { get; } = "first";

        public string SecondValue { get; } = "second";

        public string this[int index] => index switch
        {
            0 => FirstValue,
            1 => SecondValue,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

        public string this[string name] => name switch
        {
            "first" => FirstValue,
            "second" => SecondValue,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    private class StaticPropertyBase
    {
        public static string StaticProp { get; set; } = string.Empty;
    }

    private sealed class DerivedWithoutStaticProperty : StaticPropertyBase
    {
        public string InstanceProp { get; set; } = string.Empty;
    }

    private sealed class TypeWithPrivateStaticProperty : StaticPropertyBase
    {
        private static string CanWeGetPrivates { get; set; } = string.Empty;
    }

    private sealed class TypeWithPrivateStaticGetter : StaticPropertyBase
    {
        public static string ZupMan { private get; set; } = string.Empty;
    }

    private class BaseProperties
    {
        public string First { get; set; } = string.Empty;

        public string Second { get; set; } = string.Empty;
    }

    private sealed class DerivedProperties : BaseProperties
    {
        public string Third { get; set; } = string.Empty;
    }

    private interface IRootProperties
    {
        string Root { get; }
    }

    private interface ILeftProperties : IRootProperties
    {
        string Left { get; }
    }

    private interface IRightProperties : IRootProperties
    {
        string Right { get; }
    }

    private interface IDiamondProperties : ILeftProperties, IRightProperties
    {
        string Derived { get; }
    }

    private sealed class DiamondImplementation : IDiamondProperties
    {
        public string Root { get; } = string.Empty;

        public string Left { get; } = string.Empty;

        public string Right { get; } = string.Empty;

        public string Derived { get; } = string.Empty;
    }

    private interface IBaseValue
    {
        object Value { get; }
    }

    private interface IDerivedValue : IBaseValue
    {
        new string Value { get; }
    }

    private sealed class ReadAndWriteOnlyIndexers
    {
        public string this[int index] => index.ToString();

        public string this[string name]
        {
            set => _ = value ?? name;
        }
    }

    private static class StaticReadWriteShape
    {
        public static string Readable { get; } = string.Empty;

        public static string WriteOnly
        {
            set => _ = value;
        }
    }
}
