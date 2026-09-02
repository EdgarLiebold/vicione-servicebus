using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class DictionaryPropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-DICTIONARY-CONVERSION", "exact-key-value-and-nested")]
    public async Task DictionaryProperties_PreserveExactValuesAndConvertKeysValuesAndNestedContracts()
    {
        var source = new
        {
            Strings = new Dictionary<string, string>
            {
                ["Hello"] = "World",
                ["Thank You"] = "Next",
            },
            IntToStrings = new Dictionary<int, long>
            {
                [100] = 1000,
                [200] = 2000,
            },
            StringSubValues = new Dictionary<string, object>
            {
                ["A"] = new { Text = "Eh" },
                ["B"] = new { Text = "Bee" },
            },
        };

        InitializeContext<DictionaryMessage> context = await MessageInitializerCache<DictionaryMessage>.Initialize(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, context.Message.Strings.Count);
        Assert.Equal("World", context.Message.Strings["Hello"]);
        Assert.Equal("Next", context.Message.Strings["Thank You"]);
        Assert.Equal(2, context.Message.IntToStrings.Count);
        Assert.Equal("1000", context.Message.IntToStrings[100L]);
        Assert.Equal("2000", context.Message.IntToStrings[200L]);
        Assert.Equal(2, context.Message.StringSubValues.Count);
        Assert.Equal("Eh", context.Message.StringSubValues["A"].Text);
        Assert.Equal("Bee", context.Message.StringSubValues["B"].Text);
    }

    public interface DictionaryMessage
    {
        IDictionary<string, string> Strings { get; }

        IDictionary<long, string> IntToStrings { get; }

        IDictionary<string, SubValue> StringSubValues { get; }
    }

    public interface SubValue
    {
        string Text { get; }
    }
}
