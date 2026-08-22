using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Tests.Requirements;

public sealed class RequirementCoverageAttributeTests
{
    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attribute-preserves-identity")]
    public void Constructor_PreservesTheRequirementIdentity()
    {
        var attribute = new RequirementCoverageAttribute("REQ-VSB-1", "boundary-case");

        Assert.Equal("REQ-VSB-1", attribute.RequirementId);
        Assert.Equal("boundary-case", attribute.VariantKey);
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attribute-null-requirement")]
    public void NullRequirementId_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new RequirementCoverageAttribute(null!, "variant"));

    [Fact]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attribute-null-variant")]
    public void NullVariantKey_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new RequirementCoverageAttribute("REQ-1", null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attribute-blank-requirement")]
    public void BlankRequirementId_IsRejected(string value) =>
        Assert.Throws<ArgumentException>(() => new RequirementCoverageAttribute(value, "variant"));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [RequirementCoverage("REQ-TEST-REQUIREMENT-PROJECTION", "attribute-blank-variant")]
    public void BlankVariantKey_IsRejected(string value) =>
        Assert.Throws<ArgumentException>(() => new RequirementCoverageAttribute("REQ-1", value));
}
