using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Partitioning;

public sealed class Murmur3PartitionHashGeneratorTests
{
    [Theory]
    [InlineData("", 0x821f9986u)]
    [InlineData("61", 0x218e03b4u)]
    [InlineData("6162", 0xfae63edeu)]
    [InlineData("616263", 0xca19dc62u)]
    [InlineData("68656c6c6f", 0xd15f92eeu)]
    [InlineData("000102030405060708090a0b0c0d0e0f", 0xcce57279u)]
    [RequirementCoverage("REQ-VSB-PARTITION-HASH", "stable-murmur3-vectors")]
    public void ComputeHash_ReturnsStableMurmur3Values(string hexadecimalKey, uint expected)
    {
        var generator = new Murmur3PartitionHashGenerator();
        byte[] partitionKey = Convert.FromHexString(hexadecimalKey);

        uint actual = generator.ComputeHash(partitionKey);

        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PARTITION-HASH", "span-boundaries")]
    public void ComputeHash_UsesOnlyTheSuppliedSpan()
    {
        var generator = new Murmur3PartitionHashGenerator();
        byte[] paddedKey = [0xff, 0, 1, 2, 3, 4, 0xff];

        uint actual = generator.ComputeHash(paddedKey.AsSpan(1, 5));

        Assert.Equal(0xda14a2deu, actual);
    }
}
