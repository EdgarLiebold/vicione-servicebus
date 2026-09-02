using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

/// <summary>
/// The contract of <see cref="EmptyMessageBody" />: the canonical empty body answers every accessor
/// with an empty body and never with nothing.
/// </summary>
/// <remarks>
/// The product exposes a single public body instance. The test therefore verifies every observable
/// value without pretending that the accessor order can be reset on fresh instances. Object
/// identity is not asserted because it is not part of the message-body contract.
/// </remarks>
public sealed class EmptyMessageBodyTests
{
    private static readonly byte[] EmptyBytes = [];

    private const string EmptyText = "";

    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-MESSAGE-BODY", "instance-empty-body")]
    public void Instance_ExposesAnEmptyBody()
    {
        AssertExposes(EmptyMessageBody.Instance, EmptyBytes, EmptyText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-MESSAGE-BODY", "stream-read-only")]
    public void Stream_RejectsWritesAndPreservesTheBody()
    {
        var body = EmptyMessageBody.Instance;

        using (var stream = body.GetStream())
        {
            // A zero-length stream is the case where read-only matters most: the default MemoryStream
            // constructor hands out a growable buffer, so a caller could have written a body into
            // what is by definition empty.
            Assert.False(stream.CanWrite);
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0x00));
        }

        AssertExposes(body, EmptyBytes, EmptyText);
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
