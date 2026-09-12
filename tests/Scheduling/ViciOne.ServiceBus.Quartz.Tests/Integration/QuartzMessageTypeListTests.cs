using System.Text.Json;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzMessageTypeListTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "message-types-use-unambiguous-json")]
    public void SerializeAndDeserialize_RoundTripEveryIdentifierWithoutAliasingTheSource()
    {
        string[] source =
        [
            "urn:message:contracts:alpha;beta",
            "urn:message:contracts:quoted-\"value\"",
        ];

        string persisted = QuartzMessageTypeList.Serialize(source);
        source[0] = "urn:message:mutated";
        string[] restored = QuartzMessageTypeList.Deserialize(persisted);

        Assert.Equal(
            [
                "urn:message:contracts:alpha;beta",
                "urn:message:contracts:quoted-\"value\"",
            ],
            restored);
        Assert.Equal(restored, JsonSerializer.Deserialize<string[]>(persisted));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "scheduled-message-types-are-required")]
    public void Serialize_RejectsMissingOrEmptyIdentifiers()
    {
        Assert.Equal("messageTypes", Assert.Throws<ArgumentNullException>(() => QuartzMessageTypeList.Serialize(null!)).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() => QuartzMessageTypeList.Serialize([])).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() => QuartzMessageTypeList.Serialize(["valid", " "])).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[\"valid\",\"\"]")]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "persisted-message-types-fail-closed")]
    public void Deserialize_RejectsMissingOrEmptyIdentifiers(string? persisted)
    {
        Assert.Throws<FormatException>(() => QuartzMessageTypeList.Deserialize(persisted!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "persisted-message-types-require-json")]
    public void Deserialize_RejectsMalformedJson()
    {
        Assert.Throws<JsonException>(() => QuartzMessageTypeList.Deserialize("not-json"));
    }
}
