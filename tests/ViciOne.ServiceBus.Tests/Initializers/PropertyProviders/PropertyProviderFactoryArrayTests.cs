using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryArrayTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ARRAY", "exact-converted-and-nullable-targets")]
    public async Task ArraySource_SupportsExactConvertedNullableAndStringTargetsAsync()
    {
        var valuesReader = PropertyProviderTestContext.For(new IntArrayInput([1, 2, 3]));
        var nullableReader = PropertyProviderTestContext.For(new NullableIntArrayInput([1, 2, 3]));

        int[] exact = await valuesReader.ReadAsync<int[]>(nameof(IntArrayInput.Values));
        long[] converted = await valuesReader.ReadAsync<long[]>(nameof(IntArrayInput.Values));
        long?[] nullable = await valuesReader.ReadAsync<long?[]>(nameof(IntArrayInput.Values));
        string[] strings = await valuesReader.ReadAsync<string[]>(nameof(IntArrayInput.Values));
        long[] fromNullable = await nullableReader.ReadAsync<long[]>(nameof(NullableIntArrayInput.Values));

        Assert.Equal([1, 2, 3], exact);
        Assert.Equal([1L, 2L, 3L], converted);
        Assert.Equal([1L, 2L, 3L], nullable);
        Assert.Equal(["1", "2", "3"], strings);
        Assert.Equal([1L, 2L, 3L], fromNullable);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ARRAY", "nullable-element-and-enumerable-source")]
    public async Task StringAndEnumerableSources_PreserveNullableAndExactSequenceBoundariesAsync()
    {
        var stringsReader = PropertyProviderTestContext.For(new StringArrayInput(["1", "2", "", "3"]));
        var enumerableReader = PropertyProviderTestContext.For(
            new DecimalEnumerableInput(Enumerable.Repeat(98.7m, 2)));

        int?[] nullableValues = await stringsReader.ReadAsync<int?[]>(nameof(StringArrayInput.Values));
        decimal[] exactValues = await enumerableReader.ReadAsync<decimal[]>(nameof(DecimalEnumerableInput.Values));

        Assert.Equal([1, 2, null, 3], nullableValues);
        Assert.Equal([98.7m, 98.7m], exactValues);
    }

    private sealed record IntArrayInput(int[] Values);

    private sealed record NullableIntArrayInput(int?[] Values);

    private sealed record StringArrayInput(string[] Values);

    private sealed record DecimalEnumerableInput(IEnumerable<decimal> Values);
}
