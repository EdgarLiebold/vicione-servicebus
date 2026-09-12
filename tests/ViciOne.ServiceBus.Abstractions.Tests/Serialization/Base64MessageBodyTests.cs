using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class Base64MessageBodyTests
{
    private const string CarrierText = "YWJjZA==";
    private static readonly byte[] DecodedContent = [0x61, 0x62, 0x63, 0x64];

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "decoded-content-and-carrier-text")]
    public void ValidCarrier_ExposesDecodedContentAndPreservesText()
    {
        var body = new Base64MessageBody(CarrierText);

        Assert.Equal(DecodedContent.LongLength, body.Length);
        Assert.Equal(DecodedContent, body.ToArray());
        Assert.Equal(CarrierText, body.GetRequiredTransportText());
        Assert.Equal(DecodedContent, Read(body.OpenReadStream()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "empty-carrier")]
    public void EmptyCarrier_ExposesAnEmptyBody()
    {
        var body = new Base64MessageBody(string.Empty);

        Assert.Equal(0, body.Length);
        Assert.Empty(body.ToArray());
        Assert.Equal(string.Empty, body.GetRequiredTransportText());
        Assert.Empty(Read(body.OpenReadStream()));
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("Y Q==")]
    [InlineData("YQ==\r\n")]
    [InlineData("YR==")]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "eager-canonical-carrier-validation")]
    public void Constructor_RejectsMalformedOrNonCanonicalCarrierImmediately(string carrier)
    {
        Assert.Throws<FormatException>(() => new Base64MessageBody(carrier));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BASE64-MESSAGE-BODY", "required-carrier")]
    public void Constructor_RejectsMissingCarrierImmediately()
    {
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new Base64MessageBody(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-STREAM", "base64-independent-read-only-streams")]
    public void OpenReadStream_ReturnsIndependentReadOnlyStreams()
    {
        var body = new Base64MessageBody(CarrierText);
        using Stream first = body.OpenReadStream();
        using Stream second = body.OpenReadStream();

        Assert.NotSame(first, second);
        Assert.False(first.CanWrite);
        Assert.False(second.CanWrite);
        MemoryStream memoryStream = Assert.IsType<MemoryStream>(second);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        Assert.Equal(DecodedContent[0], first.ReadByte());
        Assert.Equal(0, second.Position);
        Assert.Throws<NotSupportedException>(() => second.WriteByte(0x00));
        byte[] returned = body.ToArray();
        returned.AsSpan().Fill(0x00);
        Assert.Equal(DecodedContent, body.ToArray());
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
