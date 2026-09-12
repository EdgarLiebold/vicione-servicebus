using System.Text;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

internal static class MessageBodyContractAssertions
{
    internal static void AssertExactReadOnlyBody(
        MessageBody body,
        byte[] expectedBytes,
        string expectedText,
        MessageBodyFirstAccessor firstAccessor)
    {
        ReadFirst(body, firstAccessor);

        Assert.Equal(expectedBytes.LongLength, body.Length);
        byte[] firstCopy = body.ToArray();
        Assert.Equal(expectedBytes, firstCopy);
        Assert.Equal(expectedText, body.GetRequiredTransportText());

        using Stream stream = body.OpenReadStream();
        using Stream independent = body.OpenReadStream();
        using var streamed = new MemoryStream();
        stream.CopyTo(streamed);

        Assert.Equal(expectedBytes, streamed.ToArray());
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0xFF));
        if (stream is MemoryStream memoryStream)
        {
            Assert.False(memoryStream.TryGetBuffer(out _));
            Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        }
        Assert.NotSame(stream, independent);
        Assert.Equal(0, independent.Position);
        firstCopy.AsSpan().Fill(0x00);
        Assert.Equal(expectedBytes, body.ToArray());
        Assert.Equal(expectedText, body.GetRequiredTransportText());
        Assert.Equal(expectedBytes.LongLength, body.Length);
    }

    internal static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    internal static byte[] Read(Stream stream)
    {
        using (stream)
        using (var destination = new MemoryStream())
        {
            stream.CopyTo(destination);
            return destination.ToArray();
        }
    }

    private static void ReadFirst(MessageBody body, MessageBodyFirstAccessor firstAccessor)
    {
        switch (firstAccessor)
        {
            case MessageBodyFirstAccessor.Length:
                _ = body.Length;
                break;
            case MessageBodyFirstAccessor.Bytes:
                _ = body.ToArray();
                break;
            case MessageBodyFirstAccessor.TransportText:
                _ = body.GetRequiredTransportText();
                break;
            case MessageBodyFirstAccessor.ReadStream:
                body.OpenReadStream().Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(firstAccessor), firstAccessor, null);
        }
    }
}

public enum MessageBodyFirstAccessor
{
    Length,
    Bytes,
    TransportText,
    ReadStream,
}
