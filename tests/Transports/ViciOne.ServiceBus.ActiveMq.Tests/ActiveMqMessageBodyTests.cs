using System.Reflection;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests;

public sealed class ActiveMqMessageBodyTests
{
    const string NonAsciiText = "a\u00E4\u3042b";
    static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-MESSAGE-BODY", "all-access-orders-and-unsupported-provider-types")]
    public void AllAccessOrders_UseOwnedSnapshotsAndExternalByteOracles()
    {
        ActiveMqMessageBody lengthFirst = CreateTextBody();
        Assert.Equal(NonAsciiBytes.LongLength, lengthFirst.Length);
        Assert.True(lengthFirst.TryGetPayloadText(out var textPayload));
        Assert.Equal(NonAsciiText, textPayload);
        AssertExactBody(lengthFirst, hasTransportText: true);

        ActiveMqMessageBody contentFirst = CreateBytesBody();
        Assert.Equal(NonAsciiBytes, contentFirst.ToArray());
        Assert.False(contentFirst.TryGetPayloadText(out var binaryPayload));
        Assert.Null(binaryPayload);
        AssertExactBody(contentFirst, hasTransportText: false);

        ActiveMqMessageBody textFirst = CreateTextBody();
        Assert.Equal(NonAsciiText, textFirst.GetRequiredTransportText());
        AssertExactBody(textFirst, hasTransportText: true);

        ActiveMqMessageBody streamFirst = CreateBytesBody();
        Assert.Equal(NonAsciiBytes, ReadStream(streamFirst));
        AssertExactBody(streamFirst, hasTransportText: false);

        Assert.Throws<ActiveMqTransportException>(() => new ActiveMqMessageBody(new ActiveMQMapMessage()));
    }

    [Fact]
    public void Constructor_SnapshotsTextAndBytesBeforeNativeMessagesChange()
    {
        var textMessage = new ActiveMQTextMessage { Text = NonAsciiText };
        var textBody = new ActiveMqMessageBody(textMessage);
        textMessage.Text = "changed";

        byte[] source = NonAsciiBytes.ToArray();
        var bytesMessage = new ActiveMQBytesMessage { Content = source };
        bytesMessage.Reset();
        var bytesBody = new ActiveMqMessageBody(bytesMessage);
        source[0] = 0x00;
        bytesMessage.Content[1] = 0x00;

        AssertExactBody(textBody, hasTransportText: true);
        AssertExactBody(bytesBody, hasTransportText: false);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-MESSAGE-BODY", "provider-interface-buffer-is-copied")]
    public void Constructor_CopiesAProviderOwnedByteBuffer()
    {
        byte[] providerBuffer = NonAsciiBytes.ToArray();
        IBytesMessage message = AliasingBytesMessageProxy.Create(providerBuffer);
        var body = new ActiveMqMessageBody(message);

        providerBuffer.AsSpan().Clear();

        AssertExactBody(body, hasTransportText: false);
    }

    [Fact]
    public void OpenReadStream_ReturnsIndependentReadOnlyStreams()
    {
        ActiveMqMessageBody body = CreateBytesBody();
        using Stream first = body.OpenReadStream();
        using Stream second = body.OpenReadStream();

        Assert.False(first.CanWrite);
        Assert.Throws<NotSupportedException>(() => first.WriteByte(0x00));
        MemoryStream memoryStream = Assert.IsType<MemoryStream>(first);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        Assert.Equal(NonAsciiBytes[0], first.ReadByte());
        Assert.Equal(0, second.Position);
        first.Dispose();

        Assert.Equal(NonAsciiBytes, ReadRemaining(second));
        AssertExactBody(body, hasTransportText: false);
    }

    [Fact]
    public void EmptyText_IsRepresentedByAnEmptySnapshot()
    {
        var body = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = null });

        Assert.Equal(0, body.Length);
        Assert.Empty(body.ToArray());
        Assert.Equal(string.Empty, body.GetRequiredTransportText());
        Assert.Empty(ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-MESSAGE-BODY", "required-native-message-boundary")]
    public void Constructor_RejectsMissingNativeMessageImmediately()
    {
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new ActiveMqMessageBody(null!)).ParamName);
    }

    private static ActiveMqMessageBody CreateTextBody() => new(new ActiveMQTextMessage { Text = NonAsciiText });

    private static ActiveMqMessageBody CreateBytesBody()
    {
        var message = new ActiveMQBytesMessage { Content = NonAsciiBytes.ToArray() };
        message.Reset();
        return new ActiveMqMessageBody(message);
    }

    private static void AssertExactBody(ActiveMqMessageBody body, bool hasTransportText)
    {
        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        byte[] callerCopy = body.ToArray();
        Assert.Equal(NonAsciiBytes, callerCopy);
        callerCopy.AsSpan().Clear();
        Assert.Equal(NonAsciiBytes, body.ToArray());
        Assert.Equal(hasTransportText, body.TryGetTransportText(out var transportText));
        Assert.Equal(hasTransportText ? NonAsciiText : null, transportText);
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    private static byte[] ReadStream(ActiveMqMessageBody body)
    {
        using Stream stream = body.OpenReadStream();
        return ReadRemaining(stream);
    }

    private static byte[] ReadRemaining(Stream stream)
    {
        using var result = new MemoryStream();
        stream.CopyTo(result);
        return result.ToArray();
    }

    private class AliasingBytesMessageProxy : DispatchProxy
    {
        private byte[] _content = null!;

        public static IBytesMessage Create(byte[] content)
        {
            IBytesMessage message = DispatchProxy.Create<IBytesMessage, AliasingBytesMessageProxy>();
            ((AliasingBytesMessageProxy)(object)message)._content = content;
            return message;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Content"
                ? _content
                : throw new NotSupportedException(targetMethod?.Name);
    }
}
