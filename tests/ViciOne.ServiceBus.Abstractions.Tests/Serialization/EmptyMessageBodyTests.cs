using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class EmptyMessageBodyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-MESSAGE-BODY", "canonical-empty-body")]
    public void Instance_ExposesTheCanonicalEmptyBody()
    {
        EmptyMessageBody body = EmptyMessageBody.Instance;

        Assert.Same(body, EmptyMessageBody.Instance);
        Assert.Equal(0, body.Length);
        Assert.Empty(body.ToArray());
        Assert.Equal(string.Empty, body.GetRequiredTransportText());
        using Stream stream = body.OpenReadStream();
        Assert.Equal(0, stream.Length);
        Assert.False(stream.CanWrite);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-MESSAGE-BODY", "single-public-instance")]
    public void Type_IsSealedAndHasNoPublicConstructor()
    {
        Assert.True(typeof(EmptyMessageBody).IsSealed);
        Assert.Empty(typeof(EmptyMessageBody).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-STREAM", "empty-independent-read-only-streams")]
    public void OpenReadStream_ReturnsIndependentReadOnlyStreams()
    {
        using Stream first = EmptyMessageBody.Instance.OpenReadStream();
        using Stream second = EmptyMessageBody.Instance.OpenReadStream();

        Assert.NotSame(first, second);
        Assert.False(first.CanWrite);
        Assert.False(second.CanWrite);
        Assert.Throws<NotSupportedException>(() => first.WriteByte(0x00));
        Assert.Equal(0, second.Position);
    }
}
