using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class ArrayPropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-ARRAY-CONVERSION", "scalar-exact-and-nested")]
    public async Task ArrayProperties_ConvertScalarValuesAndNestedContractsWithoutLosingOrderAsync()
    {
        var source = new
        {
            Numbers = new[] { 12, 24, 36 },
            Names = new[] { "Curly", "Larry", "Moe" },
            SubValues = new object[] { new { Text = "Frank" }, new { Text = "Lola" } },
        };

        InitializeContext<ArrayMessage> context = await MessageInitializerCache<ArrayMessage>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(["12", "24", "36"], context.Message.Numbers);
        Assert.Equal(["Curly", "Larry", "Moe"], context.Message.Names);
        Assert.Equal(2, context.Message.SubValues.Length);
        Assert.Equal("Frank", context.Message.SubValues[0].Text);
        Assert.Equal("Lola", context.Message.SubValues[1].Text);
    }

    public interface ArrayMessage
    {
        string[] Numbers { get; }

        string[] Names { get; }

        SubValue[] SubValues { get; }
    }

    public interface SubValue
    {
        string Text { get; }
    }
}
