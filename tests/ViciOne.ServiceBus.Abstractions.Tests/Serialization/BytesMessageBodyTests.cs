using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

/// <summary>
/// The contract of <see cref="BytesMessageBody" />: the bytes it was given are the body, and no
/// bytes at all is an empty body rather than a failure.
/// </summary>
/// <remarks>
/// This body hands a caller's own array straight back from <c>GetBytes</c>, so every subject below is
/// built over a fresh copy. Sharing one array between subjects would let one case decide what another
/// one sees, and the read-only stream check in particular would stop meaning anything.
/// </remarks>
public sealed class BytesMessageBodyTests
{
    /// <summary>
    /// The exact UTF-8 bytes of the four-character text below: LATIN SMALL LETTER A, LATIN SMALL
    /// LETTER A WITH DIAERESIS, HIRAGANA LETTER A, LATIN SMALL LETTER B.
    /// </summary>
    private static readonly byte[] NonAsciiBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    /// <summary>
    /// The exact text those bytes decode to. Written as escapes because this cohort asserts exact
    /// bytes, and an escape cannot be changed by whatever encoding a tool decides this file has.
    /// </summary>
    private const string NonAsciiText = "a\u00E4\u3042b";

    private static readonly byte[] EmptyBytes = [];

    private const string EmptyText = "";

    [Fact]
    [RequirementCoverage("REQ-VSB-BYTES-MESSAGE-BODY", "accessors-consistent")]
    public void Bytes_ExposeOneConsistentBody()
    {
        var lengthFirst = CreateNonAsciiSubject();
        Assert.Equal<long?>(NonAsciiBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, NonAsciiBytes, NonAsciiText);

        var bytesFirst = CreateNonAsciiSubject();
        Assert.Equal(NonAsciiBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, NonAsciiBytes, NonAsciiText);

        var textFirst = CreateNonAsciiSubject();
        Assert.Equal(NonAsciiText, textFirst.GetString());
        AssertExposes(textFirst, NonAsciiBytes, NonAsciiText);

        var streamFirst = CreateNonAsciiSubject();
        Assert.Equal(NonAsciiBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, NonAsciiBytes, NonAsciiText);

        // Two separate, otherwise identical bodies
        // report one length. The four blocks above already pin each instance to the external oracle,
        // so this line states that contract in its own right rather than being the only thing that
        // could catch a cache.
        Assert.Equal(CreateNonAsciiSubject().Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BYTES-MESSAGE-BODY", "null-input-empty-body")]
    public void NullInput_ExposesAnEmptyBody()
    {
        // No bytes is a body of length zero, not a null reference and not an exception. Every
        // accessor has to agree about that, including the one asked first.
        var lengthFirst = new BytesMessageBody(null);
        Assert.Equal<long?>(EmptyBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, EmptyBytes, EmptyText);

        var bytesFirst = new BytesMessageBody(null);
        Assert.Equal(EmptyBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, EmptyBytes, EmptyText);

        var textFirst = new BytesMessageBody(null);
        Assert.Equal(EmptyText, textFirst.GetString());
        AssertExposes(textFirst, EmptyBytes, EmptyText);

        var streamFirst = new BytesMessageBody(null);
        Assert.Equal(EmptyBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, EmptyBytes, EmptyText);

        Assert.Equal(new BytesMessageBody(null).Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BYTES-MESSAGE-BODY", "stream-read-only")]
    public void Stream_RejectsWritesAndPreservesBothBodies()
    {
        // Both inputs, because they reach the stream through different states of the same field: one
        // carries the caller's array, the other the empty array the constructor substituted. A stream
        // that became writable for only one of them would still be a route into somebody's body.
        var carried = CreateNonAsciiSubject();
        AssertStreamRejectsWrites(carried);
        AssertExposes(carried, NonAsciiBytes, NonAsciiText);

        var normalized = new BytesMessageBody(null);
        AssertStreamRejectsWrites(normalized);
        AssertExposes(normalized, EmptyBytes, EmptyText);
    }

    /// <summary>A fresh body over a fresh copy, so no subject can write into another one's array.</summary>
    private static BytesMessageBody CreateNonAsciiSubject() => new BytesMessageBody([.. NonAsciiBytes]);

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
