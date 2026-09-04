using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

/// <summary>
/// The contract of <see cref="StringMessageBody" />: the body is the UTF-8 encoding of the text it
/// was given, counted in bytes and preserved exactly.
/// </summary>
/// <remarks>
/// Three texts are held apart on purpose, because each one is a boundary the other two do not reach:
/// a text whose byte count differs from its character count, a text that consists only of whitespace,
/// and a text that is genuinely empty. Whitespace-only and empty are not the same body, and a body
/// that treated them as one would still look consistent from the inside.
/// </remarks>
public sealed class StringMessageBodyTests
{
    /// <summary>
    /// Four characters, seven UTF-8 bytes: LATIN SMALL LETTER A, LATIN SMALL LETTER A WITH DIAERESIS,
    /// HIRAGANA LETTER A, LATIN SMALL LETTER B. Written as escapes because this test asserts exact
    /// bytes, and an escape cannot be changed by whatever encoding a tool decides this file has.
    /// </summary>
    private const string NonAsciiText = "a\u00E4\u3042b";

    /// <summary>The exact UTF-8 bytes of <see cref="NonAsciiText" />, written out rather than encoded here.</summary>
    private static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    /// <summary>One space and one tab: a body made only of whitespace is still a body.</summary>
    private const string WhitespaceText = " \t";

    private static readonly byte[] WhitespaceBytes = [0x20, 0x09];

    private const string EmptyText = "";

    private static readonly byte[] EmptyBytes = [];

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "non-ascii-utf8-length")]
    public void NonAsciiText_UsesUtf8ByteLength()
    {
        // The text is four characters and seven bytes. A body that counted characters would report
        // four here and would understate every message carrying a character outside ASCII.
        var lengthFirst = new StringMessageBody(NonAsciiText);
        Assert.Equal<long?>(NonAsciiBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, NonAsciiBytes, NonAsciiText);

        var bytesFirst = new StringMessageBody(NonAsciiText);
        Assert.Equal(NonAsciiBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, NonAsciiBytes, NonAsciiText);

        var textFirst = new StringMessageBody(NonAsciiText);
        Assert.Equal(NonAsciiText, textFirst.GetString());
        AssertExposes(textFirst, NonAsciiBytes, NonAsciiText);

        var streamFirst = new StringMessageBody(NonAsciiText);
        Assert.Equal(NonAsciiBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, NonAsciiBytes, NonAsciiText);

        // Two separate, otherwise identical bodies
        // report one length. The four blocks above already pin each instance to the external oracle,
        // so this line states that contract in its own right rather than being the only thing that
        // could catch a cache.
        Assert.Equal(new StringMessageBody(NonAsciiText).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "whitespace-preserved")]
    public void WhitespaceText_IsPreserved()
    {
        // Discarding this body returned an empty array while the text accessor still returned the
        // whitespace, so the two described different bodies and the reported length belonged to
        // neither of them.
        var lengthFirst = new StringMessageBody(WhitespaceText);
        Assert.Equal<long?>(WhitespaceBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, WhitespaceBytes, WhitespaceText);

        var bytesFirst = new StringMessageBody(WhitespaceText);
        Assert.Equal(WhitespaceBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, WhitespaceBytes, WhitespaceText);

        var textFirst = new StringMessageBody(WhitespaceText);
        Assert.Equal(WhitespaceText, textFirst.GetString());
        AssertExposes(textFirst, WhitespaceBytes, WhitespaceText);

        var streamFirst = new StringMessageBody(WhitespaceText);
        Assert.Equal(WhitespaceBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, WhitespaceBytes, WhitespaceText);

        Assert.Equal(new StringMessageBody(WhitespaceText).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "empty-text-empty-body")]
    public void EmptyText_ExposesAnEmptyBody()
    {
        // Empty is its own boundary, kept apart from whitespace-only above: this body really does
        // carry nothing, and it still has to answer every accessor.
        var lengthFirst = new StringMessageBody(EmptyText);
        Assert.Equal<long?>(EmptyBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, EmptyBytes, EmptyText);

        var bytesFirst = new StringMessageBody(EmptyText);
        Assert.Equal(EmptyBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, EmptyBytes, EmptyText);

        var textFirst = new StringMessageBody(EmptyText);
        Assert.Equal(EmptyText, textFirst.GetString());
        AssertExposes(textFirst, EmptyBytes, EmptyText);

        var streamFirst = new StringMessageBody(EmptyText);
        Assert.Equal(EmptyBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, EmptyBytes, EmptyText);

        Assert.Equal(new StringMessageBody(EmptyText).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "stream-read-only")]
    public void Stream_RejectsWritesAndPreservesAllBodies()
    {
        // All three texts, because the stream hands out the cached encoded array and the three
        // arrays differ in a way that matters: multibyte content, whitespace that is easy to
        // normalize away, and a zero-length buffer whose growable variant would accept a write.
        var nonAscii = new StringMessageBody(NonAsciiText);
        AssertStreamRejectsWrites(nonAscii);
        AssertExposes(nonAscii, NonAsciiBytes, NonAsciiText);

        var whitespace = new StringMessageBody(WhitespaceText);
        AssertStreamRejectsWrites(whitespace);
        AssertExposes(whitespace, WhitespaceBytes, WhitespaceText);

        var empty = new StringMessageBody(EmptyText);
        AssertStreamRejectsWrites(empty);
        AssertExposes(empty, EmptyBytes, EmptyText);
    }

    private static void AssertStreamRejectsWrites(MessageBody body)
    {
        using var stream = body.GetStream();

        // Both halves. CanWrite is what the stream says about itself; the write is what a caller
        // would actually do, and only the second one proves the first.
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
    }

    /// <summary>
    /// Holds all four accessors against the exact external values. No product accessor is the oracle
    /// of another, so a body that answers consistently but wrongly still fails here.
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
