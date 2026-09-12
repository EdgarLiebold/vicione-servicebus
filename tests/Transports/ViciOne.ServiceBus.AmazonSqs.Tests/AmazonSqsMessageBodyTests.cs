using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsMessageBodyTests
{
    const string NonAsciiText = "a\u00E4\u3042b";
    static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];
    const string WhitespaceText = " \t";
    static readonly byte[] WhitespaceBytes = [0x20, 0x09];

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0218", "length-first-exact-utf8")]
    public void LengthFirst_EqualsExactUtf8Bytes()
    {
        SqsMessageBody body = Create(NonAsciiText);

        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        Assert.Equal(NonAsciiBytes, body.ToArray());
        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0219", "string-first-stable-length-and-bytes")]
    public void StringFirst_DoesNotChangeLengthOrBytes()
    {
        SqsMessageBody body = Create(NonAsciiText);

        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        Assert.Equal(NonAsciiBytes, body.ToArray());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0220", "non-ascii-counts-utf8-bytes")]
    public void NonAsciiLength_CountsUtf8BytesNotCharacters()
    {
        SqsMessageBody body = Create(NonAsciiText);

        Assert.Equal(4, NonAsciiText.Length);
        Assert.Equal(7, Encoding.UTF8.GetByteCount(NonAsciiText));
        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        Assert.NotEqual(NonAsciiText.Length, body.Length);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0221", "whitespace-roundtrips-exactly")]
    public void WhitespaceBody_RoundTripsExactly()
    {
        SqsMessageBody body = Create(WhitespaceText);

        Assert.Equal(WhitespaceBytes.LongLength, body.Length);
        Assert.Equal(WhitespaceBytes, body.ToArray());
        Assert.Equal(WhitespaceText, body.GetRequiredTransportText());
        Assert.Equal(WhitespaceBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0222", "read-stream-rejects-write-and-preserves-body")]
    public void ReadStream_IsReadOnlyAndBodyRemainsUnchanged()
    {
        SqsMessageBody body = Create(NonAsciiText);
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
        AssertExactBody(body);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0223", "all-access-orders-use-owned-snapshot")]
    public void AllAccessOrders_UseOwnedSnapshotAndExternalOracles()
    {
        var nativeMessage = new Message { Body = NonAsciiText };
        var body = new SqsMessageBody(nativeMessage);
        nativeMessage.Body = "changed";

        byte[] callerCopy = body.ToArray();
        callerCopy.AsSpan().Clear();

        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        Assert.Equal(NonAsciiBytes, body.ToArray());
        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MESSAGE-BODY", "invalid-utf16-is-rejected-before-size-admission")]
    public void Constructor_RejectsTextThatCannotBeEncodedAsUtf8()
    {
        var message = new Message { Body = "\ud800" };

        Assert.Throws<EncoderFallbackException>(() => new SqsMessageBody(message));
    }

    [Fact]
    public void Type_IsASealedProviderInternalMessageBody()
    {
        Type type = typeof(SqsMessageBody);

        Assert.True(type.IsNotPublic);
        Assert.True(type.IsSealed);
        Assert.Contains(typeof(MessageBody), type.GetInterfaces());
        Assert.NotEqual(typeof(StringMessageBody), type.BaseType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MESSAGE-BODY", "required-message-and-absent-native-body")]
    public void Constructor_OwnsTheNativeMessageBoundaryAndNormalizesAnAbsentBody()
    {
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new SqsMessageBody(null!)).ParamName);

        var body = new SqsMessageBody(new Message { Body = null });

        Assert.Equal(0, body.Length);
        Assert.Empty(body.ToArray());
        Assert.False(body.TryGetTransportText(out var transportText));
        Assert.Null(transportText);
        Assert.Empty(ReadStream(body));
        Assert.Null(body.GetJsonElement(JsonSerializerOptions.Default));
    }

    private static SqsMessageBody Create(string text) => new(new Message { Body = text });

    private static void AssertExactBody(MessageBody body)
    {
        Assert.Equal(NonAsciiBytes.LongLength, body.Length);
        Assert.Equal(NonAsciiBytes, body.ToArray());
        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    private static byte[] ReadStream(MessageBody body)
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
}
