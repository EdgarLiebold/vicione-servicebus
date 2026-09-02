using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class ListPropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIST-CONVERSION", "enumerable-and-array")]
    public async Task ListProperties_PreserveEnumerableAndArrayValuesInOrder()
    {
        var source = new
        {
            Amounts = Enumerable.Repeat(98.6m, 2),
            Names = new[] { "Frank", "Estelle" },
        };

        InitializeContext<ListMessage> context = await MessageInitializerCache<ListMessage>.Initialize(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal([98.6m, 98.6m], context.Message.Amounts);
        Assert.Equal(["Frank", "Estelle"], context.Message.Names);
    }

    public interface ListMessage
    {
        IList<decimal> Amounts { get; }

        IList<string> Names { get; }
    }
}
