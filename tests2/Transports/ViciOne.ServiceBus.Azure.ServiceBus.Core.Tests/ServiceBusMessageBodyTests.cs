using System.Text;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusMessageBodyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-MESSAGE-BODY", "all-access-orders-byte-length-and-readonly-stream")]
    public void EveryAccessorOrder_PreservesExactBytesAndByteLength()
    {
        byte[] expected = Encoding.UTF8.GetBytes("aäあb");
        var lengthFirst = new ServiceBusMessageBody(BinaryData.FromBytes(expected));
        var stringFirst = new ServiceBusMessageBody(BinaryData.FromBytes(expected));
        var bytesFirst = new ServiceBusMessageBody(BinaryData.FromBytes(expected));
        var streamFirst = new ServiceBusMessageBody(BinaryData.FromBytes(expected));

        long? measuredFirst = lengthFirst.Length;
        _ = stringFirst.GetString();
        _ = bytesFirst.GetBytes();
        using Stream stream = streamFirst.GetStream();
        using var copy = new MemoryStream();
        stream.CopyTo(copy);

        Assert.Equal(expected.LongLength, measuredFirst);
        Assert.Equal(expected.LongLength, stringFirst.Length);
        Assert.Equal(expected.LongLength, bytesFirst.Length);
        Assert.Equal(expected.LongLength, streamFirst.Length);
        Assert.NotEqual("aäあb".Length, lengthFirst.Length);
        Assert.Equal(expected, lengthFirst.GetBytes());
        Assert.Equal(expected, copy.ToArray());
        Assert.False(stream.CanWrite);
        Assert.Equal(0, new ServiceBusMessageBody(BinaryData.FromBytes([])).Length);
    }
}
