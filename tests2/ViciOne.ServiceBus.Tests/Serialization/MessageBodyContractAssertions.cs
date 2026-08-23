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
        Assert.Equal(expectedBytes, body.GetBytes());
        Assert.Equal(expectedText, body.GetString());

        using Stream stream = body.GetStream();
        using var streamed = new MemoryStream();
        stream.CopyTo(streamed);

        Assert.Equal(expectedBytes, streamed.ToArray());
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0xFF));
        Assert.Equal(expectedBytes, body.GetBytes());
        Assert.Equal(expectedText, body.GetString());
        Assert.Equal(expectedBytes.LongLength, body.Length);
    }

    internal static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    private static void ReadFirst(MessageBody body, MessageBodyFirstAccessor firstAccessor)
    {
        switch (firstAccessor)
        {
            case MessageBodyFirstAccessor.Length:
                _ = body.Length;
                break;
            case MessageBodyFirstAccessor.Bytes:
                _ = body.GetBytes();
                break;
            case MessageBodyFirstAccessor.String:
                _ = body.GetString();
                break;
            case MessageBodyFirstAccessor.Stream:
                body.GetStream().Dispose();
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
    String,
    Stream,
}
