using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

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
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.True(left.Equals((object)right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.Equal("retained", new Dictionary<NewIdValue, string> { [left] = "retained" }[right]);
    }

    [Theory]
    [InlineData(12, 22, 33, 44)]
    [InlineData(11, 23, 33, 44)]
    [InlineData(11, 22, 34, 44)]
    [InlineData(11, 22, 33, 45)]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "each-identity-word-controls-equality")]
    public void ChangingAnyIdentityWord_MakesTheValuesDistinct(int a, int b, int c, int d)
    {
        var left = new NewIdValue(11, 22, 33, 44);
        var right = new NewIdValue(a, b, c, d);

        Assert.False(left == right);
        Assert.True(left != right);
        Assert.False(left.Equals(right));
        Assert.False(left.Equals((object)right));
        Assert.NotEqual(0, left.CompareTo(right));
        Assert.False(new Dictionary<NewIdValue, string> { [left] = "retained" }.ContainsKey(right));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "boxed-equality-boundaries")]
    public void BoxedEquality_AcceptsEqualIdsAndRejectsOtherValues()
    {
        var value = new NewIdValue(11, 22, 33, 44);

        Assert.True(value.Equals((object)new NewIdValue(11, 22, 33, 44)));
        Assert.False(value.Equals((object)new NewIdValue(11, 22, 33, 45)));
        Assert.False(value.Equals((object?)null));
        Assert.False(value.Equals("11-22-33-44"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "boxed-ordering-boundaries")]
    public void BoxedComparison_OrdersIdsAndRejectsForeignValues()
    {
        var lower = new NewIdValue(11, 22, 33, 44);
        var higher = new NewIdValue(11, 22, 33, 45);

        Assert.Equal(0, lower.CompareTo((object)new NewIdValue(11, 22, 33, 44)));
        Assert.True(lower.CompareTo((object)higher) < 0);
        Assert.True(higher.CompareTo((object)lower) > 0);
        Assert.Equal(1, lower.CompareTo((object?)null));
        Assert.Throws<ArgumentException>(() => lower.CompareTo("11-22-33-44"));
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
