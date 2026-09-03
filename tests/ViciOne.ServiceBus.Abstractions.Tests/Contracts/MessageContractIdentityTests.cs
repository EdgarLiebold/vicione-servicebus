using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contracts;

public sealed class MessageContractIdentityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "canonical-roundtrip-and-value-semantics")]
    public void CanonicalText_RoundTripsWithoutAssemblyIdentity()
    {
        var identity = new MessageContractIdentity("vicione.devices.display-name.changed", 2);

        string text = identity.ToString();
        MessageContractIdentity parsed = MessageContractIdentity.Parse(text);

        Assert.Equal("vicione.devices.display-name.changed;v=2", text);
        Assert.Equal(identity, parsed);
        Assert.DoesNotContain(typeof(MessageContractIdentityTests).Assembly.GetName().Name!, text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has whitespace")]
    [InlineData("contains;separator")]
    [InlineData("line\nbreak")]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "name-validation")]
    public void Constructor_RejectsEveryUnsafeName(string? name)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            new MessageContractIdentity(name!, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "name-length-boundaries")]
    public void Constructor_AcceptsTheMaximumNameAndRejectsTheNextCharacter()
    {
        string maximum = new('a', 256);
        string oversized = new('a', 257);

        Assert.Equal(maximum, new MessageContractIdentity(maximum, 1).Name);
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageContractIdentity(oversized, 1));
        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "major-version-boundaries")]
    public void Constructor_RejectsEveryVersionOutsideThePersistedRange(int version)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageContractIdentity("vicione.test", version));

        Assert.Equal("majorVersion", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("vicione.test")]
    [InlineData("vicione.test;v=")]
    [InlineData(";v=1")]
    [InlineData("vicione.test;v=+1")]
    [InlineData("vicione.test;v=01")]
    [InlineData("vicione.test;v=65536")]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "strict-parse-failure")]
    public void TryParse_RejectsNonCanonicalOrInvalidTextWithoutLeakingAPartialIdentity(string? text)
    {
        bool parsed = MessageContractIdentity.TryParse(text, out MessageContractIdentity identity);

        Assert.False(parsed);
        Assert.Equal(default, identity);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-IDENTITY", "attribute-is-direct-and-validated")]
    public void Attribute_ValidatesAtConstructionAndIsNeverInherited()
    {
        AttributeUsageAttribute usage = typeof(MessageContractAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        var attribute = new MessageContractAttribute("vicione.attribute", 3);

        Assert.Equal("vicione.attribute", attribute.Name);
        Assert.Equal(3, attribute.MajorVersion);
        Assert.False(usage.Inherited);
        Assert.False(usage.AllowMultiple);
        Assert.Throws<ArgumentException>(() => new MessageContractAttribute("unsafe identity"));
    }
}
