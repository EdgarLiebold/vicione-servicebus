using MessagePack;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.MessagePack.Serialization.Formatters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessageDataFormatterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MESSAGE-DATA", "inline-bytes-and-address")]
    public async Task InlineBytes_RoundTripWithTheirReferenceAsync()
    {
        byte[] expected = [0x00, 0x7F, 0x80, 0xFF];
        var address = new Uri("loopback://message-data/inline-bytes");
        var source = new ByteMessageDataContainer
        {
            Value = new BytesInlineMessageData(expected, address),
        };

        ByteMessageDataContainer result = MessagePackRoundTrip.Execute(source);

        Assert.NotNull(result.Value);
        Assert.True(result.Value.HasValue);
        Assert.Equal(address, result.Value.Address);
        Assert.Equal(expected, await result.Value.Value);
        Assert.NotSame(expected, await result.Value.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MESSAGE-DATA", "empty-handle")]
    public void EmptyHandle_RoundTripsAsTheSharedEmptyValue()
    {
        var source = new ByteMessageDataContainer
        {
            Value = EmptyMessageData<byte[]>.Instance,
        };

        ByteMessageDataContainer result = MessagePackRoundTrip.Execute(source);
        var value = Assert.IsType<
            ViciOne.ServiceBus.Advanced.Serialization.MessageData<byte[]>>(result.Value, exactMatch: false);

        Assert.Same(EmptyMessageData<byte[]>.Instance, value);
        Assert.False(value.HasValue);
        Assert.Throws<MessageDataException>(() => value.Address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MESSAGE-DATA", "null-normalizes-to-empty-handle")]
    public void NullHandle_RoundTripsAsTheSharedEmptyValue()
    {
        var source = new ByteMessageDataContainer { Value = null };

        ByteMessageDataContainer result = MessagePackRoundTrip.Execute(source);
        var reader = new MessagePackReader(new byte[] { MessagePackCode.Nil });
        var formatter = new MessageDataFormatter<byte[]>();
        var fromWireNil = formatter.Deserialize(ref reader, MessagePackSerializationRuntime.Options);

        Assert.Same(EmptyMessageData<byte[]>.Instance, result.Value);
        Assert.Same(EmptyMessageData<byte[]>.Instance, fromWireNil);
    }

    private sealed class ByteMessageDataContainer
    {
        public ViciOne.ServiceBus.Advanced.Serialization.MessageData<byte[]>? Value { get; set; }
    }
}
