using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryDictionaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-DICTIONARY", "exact-key-and-value-conversion")]
    public async Task DictionarySource_SupportsExactAndIndependentKeyValueConversionsAsync()
    {
        var reader = PropertyProviderTestContext.For(
            new StringDictionaryInput(new Dictionary<string, string>
            {
                ["1"] = "One",
                ["2"] = "Two",
            }));

        IDictionary<string, string> exact =
            await reader.ReadAsync<IDictionary<string, string>>(nameof(StringDictionaryInput.Values));
        IDictionary<int, string> convertedKeys =
            await reader.ReadAsync<IDictionary<int, string>>(nameof(StringDictionaryInput.Values));
        IDictionary<string, object> convertedValues =
            await reader.ReadAsync<IDictionary<string, object>>(nameof(StringDictionaryInput.Values));

        Assert.Equal(2, exact.Count);
        Assert.Equal("One", exact["1"]);
        Assert.Equal("Two", exact["2"]);
        Assert.Equal(2, convertedKeys.Count);
        Assert.Equal("One", convertedKeys[1]);
        Assert.Equal("Two", convertedKeys[2]);
        Assert.Equal(2, convertedValues.Count);
        Assert.Equal("One", convertedValues["1"]);
        Assert.Equal("Two", convertedValues["2"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-DICTIONARY", "enumerable-and-nested-contract-values")]
    public async Task KeyValueEnumerableAndObjectDictionary_CreateExactDictionariesAndNestedContractsAsync()
    {
        IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string>
        {
            ["Hello"] = "World",
            ["Thank You"] = "Next",
        };
        var pairReader = PropertyProviderTestContext.For(new PairEnumerableInput(pairs));
        var objectReader = PropertyProviderTestContext.For(
            new ObjectDictionaryInput(new Dictionary<string, object>
            {
                ["Hello"] = new { Text = "Hello" },
                ["World"] = new { Text = "World" },
            }));

        IDictionary<string, string> exact =
            await pairReader.ReadAsync<IDictionary<string, string>>(nameof(PairEnumerableInput.Values));
        IDictionary<string, MessageContract> messages =
            await objectReader.ReadAsync<IDictionary<string, MessageContract>>(nameof(ObjectDictionaryInput.Values));

        Assert.Equal(2, exact.Count);
        Assert.Equal("World", exact["Hello"]);
        Assert.Equal("Next", exact["Thank You"]);
        Assert.Equal(2, messages.Count);
        Assert.Equal("Hello", messages["Hello"].Text);
        Assert.Equal("World", messages["World"].Text);
    }

    public interface MessageContract
    {
        string Text { get; }
    }

    private sealed record StringDictionaryInput(IDictionary<string, string> Values);

    private sealed record PairEnumerableInput(IEnumerable<KeyValuePair<string, string>> Values);

    private sealed record ObjectDictionaryInput(IDictionary<string, object> Values);
}
