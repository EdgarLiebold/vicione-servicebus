using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryScalarTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-SCALAR", "exact-converted-nullable-object-and-string")]
    public async Task ScalarSource_SupportsExactConvertedNullableObjectAndStringTargets()
    {
        var reader = PropertyProviderTestContext.For(new IntInput(27));
        var nullableReader = PropertyProviderTestContext.For(new NullableIntInput(27));

        int exact = await reader.ReadAsync<int>(nameof(IntInput.Value));
        long converted = await reader.ReadAsync<long>(nameof(IntInput.Value));
        int? nullableExact = await reader.ReadAsync<int?>(nameof(IntInput.Value));
        long? nullableConverted = await reader.ReadAsync<long?>(nameof(IntInput.Value));
        object boxed = await reader.ReadAsync<object>(nameof(IntInput.Value));
        string text = await reader.ReadAsync<string>(nameof(IntInput.Value));
        int fromNullable = await nullableReader.ReadAsync<int>(nameof(NullableIntInput.Value));
        long convertedFromNullable = await nullableReader.ReadAsync<long>(nameof(NullableIntInput.Value));

        Assert.Equal(27, exact);
        Assert.Equal(27L, converted);
        Assert.Equal(27, nullableExact);
        Assert.Equal(27L, nullableConverted);
        Assert.Equal(27, boxed);
        Assert.Equal("27", text);
        Assert.Equal(27, fromNullable);
        Assert.Equal(27L, convertedFromNullable);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ENUM", "enum-int-long-and-string")]
    public async Task EnumSources_FromEnumIntLongAndString_ConvergeOnTheSameValue()
    {
        TaskStatus exact = await PropertyProviderTestContext.For(new EnumInput(TaskStatus.RanToCompletion))
            .ReadAsync<TaskStatus>(nameof(EnumInput.Value));
        TaskStatus fromInt = await PropertyProviderTestContext.For(new IntInput((int)TaskStatus.RanToCompletion))
            .ReadAsync<TaskStatus>(nameof(IntInput.Value));
        TaskStatus fromLong = await PropertyProviderTestContext.For(new LongInput((long)TaskStatus.RanToCompletion))
            .ReadAsync<TaskStatus>(nameof(LongInput.Value));
        TaskStatus fromString = await PropertyProviderTestContext.For(new StringInput(nameof(TaskStatus.RanToCompletion)))
            .ReadAsync<TaskStatus>(nameof(StringInput.Value));

        Assert.Equal(TaskStatus.RanToCompletion, exact);
        Assert.Equal(TaskStatus.RanToCompletion, fromInt);
        Assert.Equal(TaskStatus.RanToCompletion, fromLong);
        Assert.Equal(TaskStatus.RanToCompletion, fromString);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-URI", "exact-and-bidirectional-string")]
    public async Task UriAndStringSources_PreserveExactAddressMeaning()
    {
        var expected = new Uri("https://service.example.test/");
        var stringReader = PropertyProviderTestContext.For(new StringInput(expected.AbsoluteUri));
        var uriReader = PropertyProviderTestContext.For(new UriInput(expected));

        Uri fromString = await stringReader.ReadAsync<Uri>(nameof(StringInput.Value));
        Uri exact = await uriReader.ReadAsync<Uri>(nameof(UriInput.Value));
        string asString = await uriReader.ReadAsync<string>(nameof(UriInput.Value));

        Assert.Equal(expected, fromString);
        Assert.Equal(expected, exact);
        Assert.Equal(expected.AbsoluteUri, asString);
    }

    private sealed record IntInput(int Value);

    private sealed record NullableIntInput(int? Value);

    private sealed record LongInput(long Value);

    private sealed record StringInput(string Value);

    private sealed record EnumInput(TaskStatus Value);

    private sealed record UriInput(Uri Value);
}
