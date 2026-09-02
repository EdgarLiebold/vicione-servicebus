using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonMessageSerializerOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OPTIONS", "compact-equivalent-wire-json")]
    public void DefaultOptions_ProduceCompactEquivalentJson()
    {
        var value = new WireJsonSample(27, "Frank", new WireJsonDetail("a longer nested value"));
        JsonSerializerOptions options = SystemTextJsonMessageSerializer.Options;
        var indentedOptions = new JsonSerializerOptions(options)
        {
            WriteIndented = true,
        };

        string compact = JsonSerializer.Serialize(value, options);
        string indented = JsonSerializer.Serialize(value, indentedOptions);
        WireJsonSample? restored = JsonSerializer.Deserialize<WireJsonSample>(compact, options);

        Assert.False(options.WriteIndented);
        Assert.True(
            Encoding.UTF8.GetByteCount(compact) < Encoding.UTF8.GetByteCount(indented),
            "The default wire representation must be smaller than its indented equivalent.");
        Assert.Equal(value, restored);
    }

    private sealed record WireJsonSample(int Id, string Customer, WireJsonDetail Detail);

    private sealed record WireJsonDetail(string Note);
}
