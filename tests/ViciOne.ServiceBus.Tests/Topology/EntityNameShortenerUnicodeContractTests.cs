using System.Text;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class EntityNameShortenerUnicodeContractTests
{
    [Theory]
    [InlineData("A😀-a-deliberately-long-entity-name", 16, "A-")]
    [InlineData("😀-a-deliberately-long-entity-name", 15, "-")]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-NAME", "bounded-name-preserves-unicode-scalar-boundaries")]
    public void Shorten_DoesNotSplitSurrogatePairAtPrefixBoundary(string original, int maximumLength, string expectedPrefix)
    {
        string shortened = EntityNameShortener.Shorten(original, maximumLength);
        string nextBudget = EntityNameShortener.Shorten(original, maximumLength + 1);

        Assert.StartsWith(expectedPrefix, shortened);
        Assert.True(shortened.Length <= maximumLength);
        Assert.Matches("^[A-Za-z0-9]*-[a-z0-9]{13}$", shortened);
        Assert.Equal(shortened[^13..], nextBudget[^13..]);
        Assert.NotEmpty(new UTF8Encoding(false, true).GetBytes(shortened));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-NAME", "temporary-name-preserves-unicode-scalar-boundaries")]
    public void TemporaryQueueName_DoesNotSplitSurrogatePairWhenTagIsTruncated()
    {
        string tag = new string('a', 13) + "😀" + new string('b', 100);

        string name = DefaultEndpointNameFormatter.GetTemporaryQueueName(tag);

        Assert.True(name.Length <= 72);
        Assert.NotEmpty(new UTF8Encoding(false, true).GetBytes(name));
    }
}
