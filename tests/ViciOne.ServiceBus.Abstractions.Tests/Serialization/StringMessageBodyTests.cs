using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class StringMessageBodyTests
{
    private const string NonAsciiText = "a\u00E4\u3042b";
    private static readonly byte[] NonAsciiContent = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "strict-utf8-content")]
    public void Text_ExposesExactUtf8ContentAndOriginalTransportText()
    {
        var body = new StringMessageBody(NonAsciiText);

        Assert.Equal(NonAsciiContent.LongLength, body.Length);
        Assert.Equal(NonAsciiContent, body.ToArray());
        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(NonAsciiText, payloadText);
        Assert.Equal(NonAsciiContent, Read(body.OpenReadStream()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "empty-and-whitespace-preserved")]
    public void EmptyAndWhitespaceText_ArePreserved(string text)
    {
        var body = new StringMessageBody(text);

        Assert.Equal(text, body.GetRequiredTransportText());
        Assert.Equal(text.Length, body.Length);
        Assert.Equal(text, System.Text.Encoding.UTF8.GetString(body.ToArray()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-STREAM", "text-independent-read-only-streams")]
    public void OpenReadStream_ReturnsIndependentReadOnlyStreams()
    {
        var body = new StringMessageBody(NonAsciiText);
        using Stream first = body.OpenReadStream();
        using Stream second = body.OpenReadStream();

        Assert.NotSame(first, second);
        Assert.False(first.CanWrite);
        Assert.False(second.CanWrite);
        MemoryStream memoryStream = Assert.IsType<MemoryStream>(second);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        Assert.Equal(NonAsciiContent[0], first.ReadByte());
        Assert.Equal(0, second.Position);
        Assert.Throws<NotSupportedException>(() => second.WriteByte(0x00));
        byte[] returned = body.ToArray();
        returned.AsSpan().Fill(0x00);
        Assert.Equal(NonAsciiContent, body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "required-text")]
    public void Constructor_RejectsMissingTextImmediately()
    {
        Assert.Equal("body", Assert.Throws<ArgumentNullException>(() => new StringMessageBody(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "internal-owned-utf8-buffer-prefix")]
    public void TakeUtf8Ownership_RetainsCapacityAndExposesOnlyIsolatedPrefixContent()
    {
        byte[] buffer = [.. NonAsciiContent, 0xFF, 0xFE];

        StringMessageBody body = StringMessageBody.TakeUtf8Ownership(buffer, NonAsciiContent.Length);

        Assert.Equal(buffer.Length, body.OwnedCapacity);
        Assert.Equal(NonAsciiContent.LongLength, body.Length);
        byte[] first = body.ToArray();
        first.AsSpan().Fill(0x00);
        Assert.Equal(NonAsciiContent, body.ToArray());
        Assert.Equal(NonAsciiText, body.GetRequiredTransportText());
        Assert.Equal(NonAsciiContent, Read(body.OpenReadStream()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STRING-MESSAGE-BODY", "internal-owned-utf8-buffer-guards")]
    public void TakeUtf8Ownership_RejectsMissingInvalidOrMalformedContent()
    {
        Assert.Equal(
            "content",
            Assert.Throws<ArgumentNullException>(() => StringMessageBody.TakeUtf8Ownership(null!, 0)).ParamName);
        Assert.Equal(
            "length",
            Assert.Throws<ArgumentOutOfRangeException>(() => StringMessageBody.TakeUtf8Ownership([], -1)).ParamName);
        Assert.Equal(
            "length",
            Assert.Throws<ArgumentOutOfRangeException>(() => StringMessageBody.TakeUtf8Ownership([], 1)).ParamName);
        Assert.Throws<System.Text.DecoderFallbackException>(() =>
            StringMessageBody.TakeUtf8Ownership([0xC3, 0x28], 2));
    }

    private static byte[] Read(Stream stream)
    {
        using (stream)
        using (var destination = new MemoryStream())
        {
            stream.CopyTo(destination);
            return destination.ToArray();
        }
    }
}
