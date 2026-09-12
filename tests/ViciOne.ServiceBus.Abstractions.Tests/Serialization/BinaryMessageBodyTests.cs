using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class BinaryMessageBodyTests
{
    private static readonly byte[] ExpectedContent = [0x61, 0xC3, 0xA4, 0xE3, 0x81, 0x82, 0x62];

    [Fact]
    [RequirementCoverage("REQ-VSB-BINARY-MESSAGE-BODY", "selected-content-owned-snapshot")]
    public void Constructor_CopiesOnlyTheSelectedContent()
    {
        byte[] source = [0xFF, .. ExpectedContent, 0xFE];
        var body = new BinaryMessageBody(source.AsMemory(1, ExpectedContent.Length));

        source.AsSpan().Fill(0x00);

        Assert.Equal(ExpectedContent.LongLength, body.Length);
        Assert.Equal(ExpectedContent, body.ToArray());
        AssertNoTransportText(body);
        Assert.Equal(ExpectedContent, Read(body.OpenReadStream()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BINARY-MESSAGE-BODY", "empty-content")]
    public void EmptyContent_ExposesOneConsistentBody()
    {
        var body = new BinaryMessageBody(ReadOnlyMemory<byte>.Empty);

        Assert.Equal(0, body.Length);
        Assert.Empty(body.ToArray());
        AssertNoTransportText(body);
        Assert.Empty(Read(body.OpenReadStream()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-STREAM", "independent-read-only-streams")]
    public void OpenReadStream_ReturnsIndependentReadOnlyStreams()
    {
        var body = new BinaryMessageBody(ExpectedContent);
        using Stream first = body.OpenReadStream();
        using Stream second = body.OpenReadStream();

        Assert.NotSame(first, second);
        Assert.Equal(0, first.Position);
        Assert.Equal(0, second.Position);
        Assert.False(first.CanWrite);
        Assert.False(second.CanWrite);
        Assert.Equal(ExpectedContent[0], first.ReadByte());
        Assert.Equal(0, second.Position);
        Assert.Throws<NotSupportedException>(() => second.WriteByte(0x00));
        var memoryStream = Assert.IsType<MemoryStream>(second);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(() => memoryStream.GetBuffer());

        first.Dispose();

        Assert.Equal(ExpectedContent, Read(second));
        Assert.Equal(ExpectedContent, body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BINARY-MESSAGE-BODY", "no-implicit-binary-text-carrier")]
    public void TransportText_RequiresAnExplicitSerializerDefinedCarrier()
    {
        var body = new BinaryMessageBody(new byte[] { 0xC3, 0x28 });

        AssertNoTransportText(body);
        Assert.Equal(new byte[] { 0xC3, 0x28 }, body.ToArray());
    }

    private static void AssertNoTransportText(BinaryMessageBody body)
    {
        Assert.False(body.TryGetTransportText(out var text));
        Assert.Null(text);
        Assert.Throws<InvalidOperationException>(body.GetRequiredTransportText);
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
