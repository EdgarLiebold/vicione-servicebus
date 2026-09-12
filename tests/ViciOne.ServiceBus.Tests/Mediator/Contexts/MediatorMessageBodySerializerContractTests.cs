using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator.Contexts;

public sealed class MediatorMessageBodySerializerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY", "canonical-json-and-owned-exact-buffer")]
    public async Task SerializeAsync_ReturnsTheCanonicalExactLengthJsonBodyAsync()
    {
        var message = new BodyMessage("Grüße", 42);
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options) { MaxDepth = 16 };
        byte[] expected = JsonSerializer.SerializeToUtf8Bytes(message, options);

        MessageBody result = await MediatorMessageBodySerializer.SerializeAsync(
            message,
            options,
            new MessageLimits { MaxBodyBytes = expected.Length, MaxEnvelopeBytes = expected.Length, MaxJsonDepth = 16 },
            new Uri("loopback://localhost/mediator"),
            TestContext.Current.CancellationToken);

        Assert.Equal(expected.Length, result.Length);
        Assert.Equal(expected, result.ToArray());
        Assert.Equal(message, JsonSerializer.Deserialize<BodyMessage>(result.ToArray(), options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY", "serializer-required-arguments")]
    public async Task SerializeAsync_RejectsEveryNullRequiredArgumentAsync()
    {
        var message = new BodyMessage("valid", 1);
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        MessageLimits limits = MessageLimits.Conservative;
        var address = new Uri("loopback://localhost/mediator");
        CancellationToken token = TestContext.Current.CancellationToken;

        await AssertParameterAsync("message", () => MediatorMessageBodySerializer.SerializeAsync<BodyMessage>(
            null!, options, limits, address, token));
        await AssertParameterAsync("serializerOptions", () => MediatorMessageBodySerializer.SerializeAsync(
            message, null!, limits, address, token));
        await AssertParameterAsync("limits", () => MediatorMessageBodySerializer.SerializeAsync(
            message, options, null!, address, token));
        await AssertParameterAsync("endpointAddress", () => MediatorMessageBodySerializer.SerializeAsync(
            message, options, limits, null!, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY-STREAM", "write-forms-and-terminal-ownership")]
    public async Task BoundedStream_SupportsEveryWriteFormAndBecomesImmutableAfterCompletionAsync()
    {
        var address = new Uri("loopback://localhost/logical");
        await using var stream = new MediatorMessageBodySerializer.BoundedMessageBodyStream(12, address);

        Assert.False(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.True(stream.CanWrite);
        Assert.Equal(0, stream.Length);
        Assert.Equal(0, stream.Position);

        stream.WriteByte((byte)'a');
        stream.Write([(byte)'b', (byte)'c']);
        stream.Write([(byte)'x', (byte)'d', (byte)'y'], 1, 1);
        await stream.WriteAsync(new ReadOnlyMemory<byte>([(byte)'e', (byte)'f']), TestContext.Current.CancellationToken);
        await stream.WriteAsync([(byte)'x', (byte)'g', (byte)'h', (byte)'y'], 1, 2, TestContext.Current.CancellationToken);
        stream.Flush();
        await stream.FlushAsync(TestContext.Current.CancellationToken);

        MessageBody body = stream.Complete();

        Assert.Equal("abcdefgh", Encoding.UTF8.GetString(body.ToArray()));
        Assert.Equal(8, body.Length);
        Assert.False(stream.CanWrite);
        Assert.Throws<InvalidOperationException>(() => stream.Complete());
        Assert.Throws<NotSupportedException>(() => stream.WriteByte((byte)'i'));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY-STREAM", "limit-and-write-boundaries")]
    public void BoundedStream_RejectsInvalidConstructionRangesAndOverflowBeforeWriting()
    {
        var address = new Uri("loopback://localhost/logical");

        Assert.Equal("maximumBytes", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MediatorMessageBodySerializer.BoundedMessageBodyStream(0, address)).ParamName);
        Assert.Equal("endpointAddress", Assert.Throws<ArgumentNullException>(() =>
            new MediatorMessageBodySerializer.BoundedMessageBodyStream(1, null!)).ParamName);

        using var stream = new MediatorMessageBodySerializer.BoundedMessageBodyStream(4, address);
        Assert.Equal("buffer", Assert.Throws<ArgumentNullException>(() => stream.Write(null!, 0, 0)).ParamName);
        Assert.Equal("offset", Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write([1], -1, 1)).ParamName);
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write([1], 0, -1)).ParamName);
        Assert.Equal("offset", Assert.Throws<ArgumentException>(() => stream.Write([1, 2], 1, 2)).ParamName);

        stream.Write([1, 2, 3, 4]);
        MessageTooLargeException failure = Assert.Throws<MessageTooLargeException>(() => stream.WriteByte(5));

        Assert.Equal(5, failure.ActualBytes);
        Assert.Equal(4, failure.MaximumBytes);
        Assert.Equal(address, failure.EndpointAddress);
        Assert.Equal(4, stream.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY-STREAM", "cancellation-and-unsupported-stream-members")]
    public async Task BoundedStream_HonorsCancellationAndRejectsUnsupportedStreamMembersAsync()
    {
        await using var stream = new MediatorMessageBodySerializer.BoundedMessageBodyStream(
            16,
            new Uri("loopback://localhost/mediator"));
        using var source = new CancellationTokenSource();
        source.Cancel();

        OperationCanceledException flushFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            stream.FlushAsync(source.Token));
        OperationCanceledException memoryFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            stream.WriteAsync(new ReadOnlyMemory<byte>([1]), source.Token).AsTask());
        OperationCanceledException arrayFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            stream.WriteAsync([1], 0, 1, source.Token));

        Assert.Equal(source.Token, flushFailure.CancellationToken);
        Assert.Equal(source.Token, memoryFailure.CancellationToken);
        Assert.Equal(source.Token, arrayFailure.CancellationToken);
        Assert.Throws<NotSupportedException>(() => stream.Position = 1);
        Assert.Throws<NotSupportedException>(() => stream.Read([0], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
    }

    private static async Task AssertParameterAsync(string expected, Func<Task> action)
    {
        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private sealed record BodyMessage(string Text, int Number);
}
