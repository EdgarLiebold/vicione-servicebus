using Apache.NMS.ActiveMQ.Commands;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

public sealed class ActiveMqMessageBodyTests
{
    private const string NonAsciiText = "a\u00E4\u3042b";
    private static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-MESSAGE-BODY", "all-access-orders-and-unsupported-provider-types")]
    public void AllAccessOrders_UseFreshMessagesAndExternalByteOracles()
    {
        ActiveMqMessageBody textLengthFirst = CreateTextBody();
        Assert.Equal<long?>(7, textLengthFirst.Length);
        AssertExactBody(textLengthFirst);

        ActiveMqMessageBody textBytesFirst = CreateTextBody();
        Assert.Equal(NonAsciiBytes, textBytesFirst.GetBytes());
        AssertExactBody(textBytesFirst);

        ActiveMqMessageBody textStringFirst = CreateTextBody();
        Assert.Equal(NonAsciiText, textStringFirst.GetString());
        AssertExactBody(textStringFirst);

        ActiveMqMessageBody textStreamFirst = CreateTextBody();
        Assert.Equal(NonAsciiBytes, ReadStream(textStreamFirst));
        AssertExactBody(textStreamFirst);

        ActiveMqMessageBody bytesLengthFirst = CreateBytesBody();
        Assert.Equal<long?>(7, bytesLengthFirst.Length);
        AssertExactBody(bytesLengthFirst);

        ActiveMqMessageBody bytesBytesFirst = CreateBytesBody();
        Assert.Equal(NonAsciiBytes, bytesBytesFirst.GetBytes());
        AssertExactBody(bytesBytesFirst);

        ActiveMqMessageBody bytesStringFirst = CreateBytesBody();
        Assert.Equal(NonAsciiText, bytesStringFirst.GetString());
        AssertExactBody(bytesStringFirst);

        ActiveMqMessageBody bytesStreamFirst = CreateBytesBody();
        Assert.Equal(NonAsciiBytes, ReadStream(bytesStreamFirst));
        AssertExactBody(bytesStreamFirst);

        var emptyText = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = null });
        Assert.Equal<long?>(0, emptyText.Length);
        Assert.Empty(emptyText.GetBytes());
        Assert.Equal(string.Empty, emptyText.GetString());

        var unsupported = new ActiveMqMessageBody(new ActiveMQMapMessage());
        Assert.Throws<ActiveMqTransportException>(() => unsupported.Length);
        Assert.Throws<ActiveMqTransportException>(() => unsupported.GetBytes());
        Assert.Throws<ActiveMqTransportException>(() => unsupported.GetString());
        Assert.Throws<ActiveMqTransportException>(() => unsupported.GetStream());
    }

    private static ActiveMqMessageBody CreateTextBody() => new(new ActiveMQTextMessage { Text = NonAsciiText });

    private static ActiveMqMessageBody CreateBytesBody()
    {
        var message = new ActiveMQBytesMessage { Content = NonAsciiBytes.ToArray() };
        message.Reset();
        return new ActiveMqMessageBody(message);
    }

    private static void AssertExactBody(ActiveMqMessageBody body)
    {
        Assert.Equal<long?>(NonAsciiBytes.LongLength, body.Length);
        Assert.Equal(NonAsciiBytes, body.GetBytes());
        Assert.Equal(NonAsciiText, body.GetString());

        using Stream stream = body.GetStream();
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
        using var result = new MemoryStream();
        stream.CopyTo(result);
        Assert.Equal(NonAsciiBytes, result.ToArray());
    }

    private static byte[] ReadStream(ActiveMqMessageBody body)
    {
        using Stream stream = body.GetStream();
        using var result = new MemoryStream();
        stream.CopyTo(result);
        return result.ToArray();
    }
}
