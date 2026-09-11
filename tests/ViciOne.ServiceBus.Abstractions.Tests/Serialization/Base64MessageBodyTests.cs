using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

/// <summary>
/// The contract of <see cref="Base64MessageBody" />: the text is what a text transport carries, the
/// decoded bytes are the body, and the two have different lengths.
/// </summary>
/// <remarks>
/// This body is the reason a length is not a character count. The encoded text is about a third
/// longer than the body it carries, so the two oracles below deliberately disagree in size: a body
/// that reported its text length would look plausible and be wrong by exactly that difference.
/// </remarks>
public sealed class Base64MessageBodyTests
{
    /// <summary>Eight characters of Base64 carrying four bytes, so text and body lengths differ.</summary>
    private const string ValidText = "YWJjZA==";

    /// <summary>The exact bytes <see cref="ValidText" /> encodes: the ASCII letters a, b, c and d.</summary>
    private static readonly byte[] ValidBytes = [0x61, 0x62, 0x63, 0x64];

    /// <summary>Empty Base64 text. Decoding it is legal and yields an empty body rather than a failure.</summary>
    private const string EmptyText = "";

    private static readonly byte[] EmptyBytes = [];

    /// <summary>
    /// Invalid twice over: the hyphen is outside the Base64 alphabet and the length is not a multiple
    /// of four, so neither reason alone decides the outcome.
    /// </summary>
    private const string InvalidText = "not-base64";

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "valid-text-decoded-body")]
    public void ValidText_ExposesDecodedBytesAndOriginalText()
    {
        var lengthFirst = new Base64MessageBody(ValidText);
        Assert.Equal<long?>(ValidBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, ValidBytes, ValidText);

        var bytesFirst = new Base64MessageBody(ValidText);
        Assert.Equal(ValidBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, ValidBytes, ValidText);

        var textFirst = new Base64MessageBody(ValidText);
        Assert.Equal(ValidText, textFirst.GetString());
        AssertExposes(textFirst, ValidBytes, ValidText);

        var streamFirst = new Base64MessageBody(ValidText);
        Assert.Equal(ValidBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, ValidBytes, ValidText);

        // Two separate, otherwise identical bodies
        // report one length. The four blocks above already pin each instance to the external oracle,
        // so this line states that contract in its own right rather than being the only thing that
        // could catch a cache.
        Assert.Equal(new Base64MessageBody(ValidText).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "empty-text-empty-body")]
    public void EmptyText_ExposesAnEmptyBody()
    {
        var lengthFirst = new Base64MessageBody(EmptyText);
        Assert.Equal<long?>(EmptyBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, EmptyBytes, EmptyText);

        var bytesFirst = new Base64MessageBody(EmptyText);
        Assert.Equal(EmptyBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, EmptyBytes, EmptyText);

        var textFirst = new Base64MessageBody(EmptyText);
        Assert.Equal(EmptyText, textFirst.GetString());
        AssertExposes(textFirst, EmptyBytes, EmptyText);

        var streamFirst = new Base64MessageBody(EmptyText);
        Assert.Equal(EmptyBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, EmptyBytes, EmptyText);

        Assert.Equal(new Base64MessageBody(EmptyText).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "invalid-text-format-error")]
    public void InvalidText_ThrowsFormatExceptionForBinaryAccess()
    {
        // One fresh body per access. The decoded array is cached on first success, so a shared body
        // would let whichever accessor ran first decide what the others see, and three accesses would
        // prove one.
        var lengthSubject = new Base64MessageBody(InvalidText);
        Assert.Throws<FormatException>(() => { _ = lengthSubject.Length; });

        var bytesSubject = new Base64MessageBody(InvalidText);
        Assert.Throws<FormatException>(() => { _ = bytesSubject.GetBytes(); });

        var streamSubject = new Base64MessageBody(InvalidText);
        Assert.Throws<FormatException>(() => { _ = streamSubject.GetStream(); });

        // Only the binary view fails. The text a text transport carries is still readable, and
        // returning it is what lets a caller see what arrived.
        Assert.Equal(InvalidText, new Base64MessageBody(InvalidText).GetString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "stream-read-only")]
    public void Stream_RejectsWritesAndPreservesTheBody()
    {
        var body = new Base64MessageBody(ValidText);

        using (var stream = body.GetStream())
        {
            // Both halves. CanWrite is what the stream says about itself; the write is what a caller
            // would actually do, and only the second one proves the first.
            Assert.False(stream.CanWrite);
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
        }

        // The stream is a window onto the cached decoded array, so a write that got through would
        // change what every later accessor of this body reads.
        AssertExposes(body, ValidBytes, ValidText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "required-text-boundary")]
    public void Constructor_RejectsMissingTextImmediately()
    {
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new Base64MessageBody(null!)).ParamName);
    }

    /// <summary>
    /// Holds all four accessors against the exact external values. The text oracle is the encoded
    /// text and the byte oracle is the decoded body, so neither accessor can stand in for the other.
    /// </summary>
    private static void AssertExposes(MessageBody body, byte[] expectedBytes, string expectedText)
    {
        Assert.Equal<long?>(expectedBytes.LongLength, body.Length);
        Assert.Equal(expectedBytes, body.GetBytes());
        Assert.Equal(expectedText, body.GetString());
        Assert.Equal(expectedBytes, ReadStream(body));
    }

    private static byte[] ReadStream(MessageBody body)
    {
        using var stream = body.GetStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
