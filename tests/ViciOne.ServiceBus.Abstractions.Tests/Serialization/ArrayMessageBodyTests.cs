using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

/// <summary>
/// The contract of <see cref="ArrayMessageBody" />: the body is the selected segment, never the
/// array that happens to carry it.
/// </summary>
/// <remarks>
/// The segment is placed between a nonempty prefix and a nonempty suffix, so offset and count both
/// have to be honoured by every accessor. A body that ignored either would still answer consistently
/// - it would simply describe the wrong bytes - which is why the oracles below are exact external
/// values rather than one accessor checked against another.
/// </remarks>
public sealed class ArrayMessageBodyTests
{
    /// <summary>
    /// Four characters, seven UTF-8 bytes: LATIN SMALL LETTER A, LATIN SMALL LETTER A WITH DIAERESIS,
    /// HIRAGANA LETTER A, LATIN SMALL LETTER B. Written as escapes because this test asserts exact
    /// bytes, and an escape cannot be changed by whatever encoding a tool decides this file has.
    /// </summary>
    private const string SelectedText = "a\u00E4\u3042b";

    /// <summary>The exact UTF-8 bytes of <see cref="SelectedText" />, written out rather than encoded here.</summary>
    private static readonly byte[] SelectedBytes = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    /// <summary>
    /// Bytes that are never valid UTF-8 lead or continuation bytes here, so any prefix or suffix that
    /// leaked into the text view turns into replacement characters instead of reading plausibly.
    /// </summary>
    private static readonly byte[] Prefix = [0xFF, 0xFE];

    private static readonly byte[] Suffix = [0xFD];

    [Fact]
    [RequirementCoverage("REQ-VSB-ARRAY-MESSAGE-BODY", "segment-accessors-consistent")]
    public void SegmentAccessors_ExposeOnlyTheSelectedBytes()
    {
        var lengthFirst = CreateSubject();
        Assert.Equal<long?>(SelectedBytes.LongLength, lengthFirst.Length);
        AssertExposes(lengthFirst, SelectedBytes, SelectedText);

        var bytesFirst = CreateSubject();
        Assert.Equal(SelectedBytes, bytesFirst.GetBytes());
        AssertExposes(bytesFirst, SelectedBytes, SelectedText);

        var textFirst = CreateSubject();
        Assert.Equal(SelectedText, textFirst.GetString());
        AssertExposes(textFirst, SelectedBytes, SelectedText);

        var streamFirst = CreateSubject();
        Assert.Equal(SelectedBytes, ReadStream(streamFirst));
        AssertExposes(streamFirst, SelectedBytes, SelectedText);

        // Two separate, otherwise identical bodies
        // report one length. The four blocks above already pin each instance to the external oracle,
        // so this line states that contract in its own right rather than being the only thing that
        // could catch a cache.
        Assert.Equal(CreateSubject().Length, streamFirst.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ARRAY-MESSAGE-BODY", "stream-read-only")]
    public void Stream_RejectsWritesAndPreservesTheBody()
    {
        var body = CreateSubject();

        using (var stream = body.GetStream())
        {
            // Both halves. CanWrite is what the stream says about itself; the write is what a caller
            // would actually do, and only the second one proves the first.
            Assert.False(stream.CanWrite);
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
        }

        // The refused write may not have landed anywhere. This stream is a window onto the caller's
        // own array, so a write that got through would change what every other accessor reads.
        AssertExposes(body, SelectedBytes, SelectedText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ARRAY-MESSAGE-BODY", "default-segment-is-consistently-empty")]
    public void DefaultSegment_ExposesOneConsistentEmptyBody()
    {
        var body = new ArrayMessageBody(default);

        AssertExposes(body, [], string.Empty);
    }

    /// <summary>A fresh body over a fresh backing array, selected at a nonzero offset.</summary>
    private static ArrayMessageBody CreateSubject() =>
        new(new ArraySegment<byte>(BackingArray(), Prefix.Length, SelectedBytes.Length));

    /// <summary>
    /// Composed from the three parts rather than written out again, so the segment bounds cannot
    /// drift away from the bytes they are supposed to select.
    /// </summary>
    private static byte[] BackingArray() => [.. Prefix, .. SelectedBytes, .. Suffix];

    /// <summary>
    /// Holds all four accessors against the exact external values. No product accessor is the oracle
    /// of another, so a body that answers consistently but wrongly still fails here.
    /// </summary>
    private static void AssertExposes(MessageBody body, byte[] expectedBytes, string expectedText)
    {
        // The interface declares a nullable length, and the comparison keeps that shape: a body that
        // answered null would be a contract breach rather than a value to unwrap.
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
