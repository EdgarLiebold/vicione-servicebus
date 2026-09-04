using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

public sealed class NewIdValueTests
{
    [Theory]
    [InlineData(null, "00000000-0000-0000-0000-000000000000")]
    [InlineData("B", "{00000000-0000-0000-0000-000000000000}")]
    [InlineData("N", "00000000000000000000000000000000")]
    [InlineData("P", "(00000000-0000-0000-0000-000000000000)")]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "empty-guid-format-parity")]
    public void EmptyValue_UsesGuidCompatibleFormatting(string? format, string expected)
    {
        var id = default(NewIdValue);

        Assert.Equal(expected, format is null ? id.ToString() : id.ToString(format));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "byte-construction")]
    public void GuidBytes_PreserveTheGuidRepresentation()
    {
        var guid = Guid.Parse("f6b27c7c-8ab8-4498-ac97-3a6107a21320");

        var id = new NewIdValue(guid.ToByteArray());

        Assert.Equal(guid.ToString("D"), id.ToString("D"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "equality")]
    public void EqualValues_CompareEqual()
    {
        var left = new NewIdValue("fc070000-9565-3668-e000-08d5893343c6");
        var right = new NewIdValue("fc070000-9565-3668-e000-08d5893343c6");

        Assert.Equal(left, right);
        Assert.Equal(0, left.CompareTo(right));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "strict-order")]
    public void EarlierValue_ComparesLower()
    {
        var lower = new NewIdValue("fc070000-9565-3668-e000-08d5893343c6");
        var higher = new NewIdValue("fc070000-9565-3668-9180-08d589338b38");

        Assert.True(lower.CompareTo(higher) < 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "non-strict-order")]
    public void EarlierValue_SatisfiesNonStrictOrdering()
    {
        var lower = new NewIdValue("fc070000-9565-3668-e000-08d5893343c6");
        var higher = new NewIdValue("fc070000-9565-3668-9180-08d589338b38");

        Assert.True(lower.CompareTo(higher) <= 0);
    }
}
