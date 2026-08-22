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
}
