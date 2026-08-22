using MessagePack;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class InternalMessagePackResolverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SECURITY", "untrusted-data-mode")]
    public void Options_UseUntrustedDataSecurity()
    {
        Assert.Equal(MessagePackSecurity.UntrustedData, InternalMessagePackResolver.Options.Security);
        Assert.True(InternalMessagePackResolver.Options.Security.HashCollisionResistant);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SECURITY", "hardened-dictionary-read")]
    public void HardenedOptions_ReadTheOverlayDictionaryShape()
    {
        var bytes = InternalMessagePackResolver.Serialize(
            new Dictionary<string, object> { ["id"] = 27, ["customer"] = "Frank" });

        var result = InternalMessagePackResolver.Deserialize<Dictionary<string, object>>(bytes);

        Assert.Equal(27, result["id"]);
        Assert.Equal("Frank", result["customer"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SERIALIZER", "object-buffer-inputs")]
    public void ObjectBufferNormalization_AcceptsBytesBase64AndObjects()
    {
        var source = new BufferValue { Id = 27 };
        byte[] serialized = InternalMessagePackResolver.Serialize(source);

        byte[] fromBytes = MessagePackMessageSerializer.EnsureObjectBufferFormatIsByteArray(serialized);
        byte[] fromBase64 = MessagePackMessageSerializer.EnsureObjectBufferFormatIsByteArray(
            Convert.ToBase64String(serialized));
        byte[] fromObject = MessagePackMessageSerializer.EnsureObjectBufferFormatIsByteArray(source);

        Assert.Same(serialized, fromBytes);
        Assert.Equal(serialized, fromBase64);
        Assert.Equal(27, InternalMessagePackResolver.Deserialize<BufferValue>(fromObject).Id);
    }

    public sealed class BufferValue
    {
        public int Id { get; set; }
    }
}
