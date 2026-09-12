using System.Text;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusMessageBodyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-MESSAGE-BODY", "all-access-orders-byte-length-and-readonly-stream")]
    public void EveryAccessorOrder_PreservesExactBytesAndByteLength()
    {
        const string expectedText = "aäあb";
        byte[] expected = Encoding.UTF8.GetBytes(expectedText);
        byte[] source = expected.ToArray();
        var body = new ServiceBusMessageBody(BinaryData.FromBytes(source));
        source[0] = 0x00;

        Assert.Equal(expected.LongLength, body.Length);
        Assert.NotEqual(expectedText.Length, body.Length);
        byte[] callerCopy = body.ToArray();
        Assert.Equal(expected, callerCopy);
        callerCopy.AsSpan().Clear();
        Assert.Equal(expected, body.ToArray());
        Assert.False(body.TryGetTransportText(out var transportText));
        Assert.Null(transportText);

        using Stream first = body.OpenReadStream();
        using Stream second = body.OpenReadStream();
        Assert.False(first.CanWrite);
        Assert.Throws<NotSupportedException>(() => first.WriteByte(0x00));
        MemoryStream memoryStream = Assert.IsType<MemoryStream>(first);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        Assert.Equal(expected[0], first.ReadByte());
        Assert.Equal(0, second.Position);
        first.Dispose();

        Assert.Equal(expected, ReadRemaining(second));
        Assert.Equal(expected, body.ToArray());
    }

    [Fact]
    public void EmptyAndMalformedBodies_RemainOpaqueBinaryContent()
    {
        var empty = new ServiceBusMessageBody(BinaryData.FromBytes([]));
        var malformed = new ServiceBusMessageBody(BinaryData.FromBytes([0xC3, 0x28]));

        Assert.Equal(0, empty.Length);
        Assert.Empty(empty.ToArray());
        Assert.False(empty.TryGetTransportText(out var emptyText));
        Assert.Null(emptyText);
        Assert.False(malformed.TryGetTransportText(out var malformedText));
        Assert.Null(malformedText);
        Assert.Equal(new byte[] { 0xC3, 0x28 }, malformed.ToArray());
    }

    [Fact]
    public void Type_IsProviderInternalAndSealed()
    {
        Type type = typeof(ServiceBusMessageBody);

        Assert.True(type.IsNotPublic);
        Assert.True(type.IsSealed);
        Assert.Contains(typeof(MessageBody), type.GetInterfaces());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-MESSAGE-BODY", "required-binary-data-boundary")]
    public void Constructor_RejectsMissingBinaryDataImmediately()
    {
        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() => new ServiceBusMessageBody(null!)).ParamName);
    }

    private static byte[] ReadRemaining(Stream stream)
    {
        using var result = new MemoryStream();
        stream.CopyTo(result);
        return result.ToArray();
    }
}
