namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

using global::ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class TypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(TypeWithTwoIndexers), 4)]
    [InlineData(typeof(DerivedTypeWithIndexer), 4)]
    [RequirementCoverage("REQ-VSB-TYPE-EXTENSIONS", "multiple-indexers")]
    public void GetAllProperties_EnumeratesTypesWithDifferentIndexerSignatures(Type type, int expectedCount)
    {
        var properties = type.GetAllProperties().ToArray();

        Assert.Equal(expectedCount, properties.Length);
        Assert.Equal(2, properties.Count(property => property.GetIndexParameters().Length == 1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "private-getter-plus-inherited")]
    public void GetAllStaticProperties_IncludesPrivateGetterAndInheritedProperty()
    {
        var propertyNames = typeof(TypeWithPrivateStaticGetter).GetAllStaticProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["StaticProp", "ZupMan"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "private-property-plus-inherited")]
    public void GetAllStaticProperties_IncludesPrivateAndInheritedProperties()
    {
        var propertyNames = typeof(TypeWithPrivateStaticProperty).GetAllStaticProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["CanWeGetPrivates", "StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "standalone-static-property")]
    public void GetAllStaticProperties_StandaloneTypeReturnsItsStaticProperty()
    {
        var propertyNames = typeof(StaticPropertyBase).GetAllStaticProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "inherited-static-property")]
    public void GetAllStaticProperties_DerivedTypeReturnsInheritedStaticProperty()
    {
        var propertyNames = typeof(DerivedWithoutStaticProperty).GetAllStaticProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["StaticProp"], propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATIC-PROPERTY-REFLECTION", "declared-only-static-property")]
    public void GetStaticProperties_ExcludesInheritedStaticProperty()
    {
        var propertyNames = typeof(TypeWithPrivateStaticGetter).GetStaticProperties()
            .Select(property => property?.Name
                ?? throw new InvalidOperationException("The reflection helper returned a null property."))
            .ToArray();

        Assert.Equal(["ZupMan"], propertyNames);
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
}
