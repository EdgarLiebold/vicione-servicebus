using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsMessageBodyTests
{
    private const string NonAsciiText = "a\u00E4\u3042b";
    private static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];
    private const string WhitespaceText = " \t";
    private static readonly byte[] WhitespaceBytes = [0x20, 0x09];

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0218", "length-first-exact-utf8")]
    public void LengthFirst_EqualsExactUtf8Bytes()
    {
        var body = Create(NonAsciiText);

        Assert.Equal<long?>(7, body.Length);
        Assert.Equal(NonAsciiBytes, body.GetBytes());
        Assert.Equal(NonAsciiText, body.GetString());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0219", "string-first-stable-length-and-bytes")]
    public void StringFirst_DoesNotChangeLengthOrBytes()
    {
        var body = Create(NonAsciiText);

        Assert.Equal(NonAsciiText, body.GetString());
        Assert.Equal<long?>(7, body.Length);
        Assert.Equal(NonAsciiBytes, body.GetBytes());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0220", "non-ascii-counts-utf8-bytes")]
    public void NonAsciiLength_CountsUtf8BytesNotCharacters()
    {
        var body = Create(NonAsciiText);

        Assert.Equal(4, NonAsciiText.Length);
        Assert.Equal(7, Encoding.UTF8.GetByteCount(NonAsciiText));
        Assert.Equal<long?>(NonAsciiBytes.LongLength, body.Length);
        Assert.NotEqual<long?>(NonAsciiText.Length, body.Length);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0221", "whitespace-roundtrips-exactly")]
    public void WhitespaceBody_RoundTripsExactly()
    {
        var body = Create(WhitespaceText);

        Assert.Equal<long?>(WhitespaceBytes.LongLength, body.Length);
        Assert.Equal(WhitespaceBytes, body.GetBytes());
        Assert.Equal(WhitespaceText, body.GetString());
        Assert.Equal(WhitespaceBytes, ReadStream(body));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0222", "read-stream-rejects-write-and-preserves-body")]
    public void ReadStream_IsReadOnlyAndBodyRemainsUnchanged()
    {
        var body = Create(NonAsciiText);
        using Stream stream = body.GetStream();

        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
        Assert.Equal(NonAsciiBytes, body.GetBytes());
        Assert.Equal(NonAsciiText, body.GetString());
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0223", "all-inherited-access-orders-use-external-oracles")]
    public void AllInheritedAccessOrders_UseFreshBodiesAndExternalOracles()
    {
        var lengthFirst = Create(NonAsciiText);
        Assert.Equal<long?>(7, lengthFirst.Length);
        AssertExactBody(lengthFirst);

        var bytesFirst = Create(NonAsciiText);
        Assert.Equal(NonAsciiBytes, bytesFirst.GetBytes());
        AssertExactBody(bytesFirst);

        var stringFirst = Create(NonAsciiText);
        Assert.Equal(NonAsciiText, stringFirst.GetString());
        AssertExactBody(stringFirst);

        var streamFirst = Create(NonAsciiText);
        Assert.Equal(NonAsciiBytes, ReadStream(streamFirst));
        AssertExactBody(streamFirst);

        const BindingFlags OwnMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.Equal(typeof(StringMessageBody), typeof(SqsMessageBody).BaseType);
        Assert.Empty(typeof(SqsMessageBody).GetMember(nameof(MessageBody.Length), OwnMembers));
        Assert.Empty(typeof(SqsMessageBody).GetMember(nameof(MessageBody.GetBytes), OwnMembers));
        Assert.Empty(typeof(SqsMessageBody).GetMember(nameof(MessageBody.GetString), OwnMembers));
        Assert.Empty(typeof(SqsMessageBody).GetMember(nameof(MessageBody.GetStream), OwnMembers));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MESSAGE-BODY", "required-message-and-absent-native-body")]
    public void Constructor_OwnsTheNativeMessageBoundaryAndNormalizesAnAbsentBody()
    {
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new SqsMessageBody(null!)).ParamName);

        var body = new SqsMessageBody(new Message { Body = null });

        Assert.Equal<long?>(0, body.Length);
        Assert.Empty(body.GetBytes());
        Assert.Equal(string.Empty, body.GetString());
        Assert.Null(body.GetJsonElement(JsonSerializerOptions.Default));
    }

    private static SqsMessageBody Create(string text) => new(new Message { Body = text });

    private static void AssertExactBody(MessageBody body)
    {
        Assert.Equal<long?>(7, body.Length);
        Assert.Equal(NonAsciiBytes, body.GetBytes());
        Assert.Equal(NonAsciiText, body.GetString());
        Assert.Equal(NonAsciiBytes, ReadStream(body));
    }

    private static byte[] ReadStream(MessageBody body)
    {
        using Stream stream = body.GetStream();
        using var result = new MemoryStream();
        stream.CopyTo(result);
        return result.ToArray();
    }
}
