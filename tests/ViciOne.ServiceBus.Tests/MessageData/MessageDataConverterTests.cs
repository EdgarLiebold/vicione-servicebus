using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "built-in-converter-null-and-cancellation-boundaries")]
    public async Task BuiltInConverters_OwnTheirRequiredStreamAndCancellationBoundariesAsync()
    {
        var bytes = new ByteArrayMessageDataConverter();
        var text = new StringMessageDataConverter();
        var stream = new StreamMessageDataConverter();
        var json = new SystemTextJsonObjectMessageDataConverter<Payload>(new JsonSerializerOptions());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            new SystemTextJsonObjectMessageDataConverter<Payload>(null!)).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            bytes.ConvertAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            text.ConvertAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            stream.ConvertAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            json.ConvertAsync(null!, CancellationToken.None))).ParamName);

        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("value"), writable: false);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            text.ConvertAsync(source, cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "stream-converter-subtype-retains-source")]
    public async Task StreamConverterCapability_TransfersTheRepositoryStreamWithoutPrematureDisposalAsync()
    {
        var source = new TrackingStream([1, 2, 3]);
        var repository = new SingleStreamRepository(source);
        var data = new GetMessageData<Stream>(
            new Uri("urn:message-data:stream"),
            repository,
            new OwnershipTransferringStreamConverter(),
            TestContext.Current.CancellationToken);

        Stream result = Assert.IsAssignableFrom<Stream>(await data.Value);

        Assert.Same(source, result);
        Assert.False(source.IsDisposed);
        await result.DisposeAsync();
        Assert.True(source.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "inline-byte-input-and-output-snapshots")]
    public async Task InlineBytes_DoNotExposeCallerOwnedMutableArraysAsync()
    {
        byte[] input = [1, 2, 3];
        var data = new BytesInlineMessageData(input);
        input[0] = 99;

        byte[] first = Assert.IsType<byte[]>(await data.Value);
        first[1] = 88;
        byte[] second = Assert.IsType<byte[]>(await data.Value);

        Assert.Equal([1, 2, 3], second);
    }

    private sealed record Payload(string Value);

    private sealed class OwnershipTransferringStreamConverter : IMessageDataConverter<Stream>
    {
        public bool TransfersSourceStreamOwnership => true;

        public Task<Stream?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream?>(stream);
        }
    }

    private sealed class SingleStreamRepository(TrackingStream stream) : IMessageDataRepository
    {
        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(stream);
        }

        public Task<Uri> PutAsync(Stream value, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TrackingStream(byte[] value) : MemoryStream(value, writable: false)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
