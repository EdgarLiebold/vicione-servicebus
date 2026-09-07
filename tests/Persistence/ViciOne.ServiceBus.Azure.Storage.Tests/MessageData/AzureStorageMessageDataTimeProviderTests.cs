using System.Net;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Azure.Storage;
using ViciOne.ServiceBus.Azure.Storage.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Storage.Tests.MessageData;

public sealed class AzureStorageMessageDataTimeProviderTests
{
    private static readonly DateTimeOffset Now = new(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [Theory]
    [InlineData(5, 5)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "ttl-and-minimum-expiration-use-injected-clock")]
    public async Task TimeToLiveMetadata_UsesTheInjectedClockAndMinimumExpirationAsync(int requestedMinutes, int expectedMinutes)
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));

        Uri address = await repository.PutAsync(
            new MemoryStream([1, 2, 3]),
            TimeSpan.FromMinutes(requestedMinutes),
            TestContext.Current.CancellationToken);

        Assert.StartsWith("https://clock.blob.core.windows.net/message-data/", address.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal(2, handler.Requests.Count);
        RecordedRequest metadata = Assert.Single(handler.Requests, request => request.IsMetadata);
        Assert.Equal(HttpMethod.Put, metadata.Method);
        Assert.Equal((Now.UtcDateTime + TimeSpan.FromMinutes(expectedMinutes)).ToString("O"), metadata.ValidUntilUtc);
        Assert.True(metadata.CancellationToken.CanBeCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "missing-ttl-does-not-write-expiration-metadata")]
    public async Task MissingTimeToLive_DoesNotIssueAMetadataWriteAsync()
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));

        await repository.PutAsync(new MemoryStream([4, 5, 6]), cancellationToken: TestContext.Current.CancellationToken);

        RecordedRequest upload = Assert.Single(handler.Requests);
        Assert.False(upload.IsMetadata);
        Assert.Null(upload.ValidUntilUtc);
        Assert.True(upload.CancellationToken.CanBeCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "metadata-write-honors-cancellation")]
    public async Task TimeToLiveMetadata_HonorsCallerCancellationAsync()
    {
        var handler = new RecordingBlobHandler { BlockMetadata = true };
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));
        using var cancellation = new CancellationTokenSource();

        Task<Uri> put = repository.PutAsync(
            new MemoryStream([7, 8, 9]),
            TimeSpan.FromMinutes(5),
            cancellation.Token);
        await handler.MetadataStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        Task completed = await Task.WhenAny(
            put,
            Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
        handler.ReleaseMetadata();

        Assert.Same(put, completed);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => put);
    }

    private static AzureBlobMessageDataRepository CreateRepository(HttpMessageHandler handler, TimeProvider timeProvider)
    {
        var options = new BlobClientOptions
        {
            Transport = new HttpClientTransport(handler),
        };
        options.Retry.MaxRetries = 0;
        var client = new BlobServiceClient(new Uri("https://clock.blob.core.windows.net"), options);
        return client.CreateMessageDataRepository("message-data", timeProvider: timeProvider);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class RecordingBlobHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _metadataRelease = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockMetadata { get; init; }

        public TaskCompletionSource MetadataStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public List<RecordedRequest> Requests { get; } = [];

        public void ReleaseMetadata() => _metadataRelease.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            bool isMetadata = request.RequestUri?.Query.Contains("comp=metadata", StringComparison.Ordinal) == true;
            string? validUntilUtc = request.Headers.TryGetValues("x-ms-meta-ValidUntilUtc", out IEnumerable<string>? values)
                ? Assert.Single(values)
                : null;
            Requests.Add(new RecordedRequest(request.Method, isMetadata, validUntilUtc, cancellationToken));

            if (isMetadata && BlockMetadata)
            {
                MetadataStarted.TrySetResult();
                await _metadataRelease.Task.WaitAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(isMetadata ? HttpStatusCode.OK : HttpStatusCode.Created)
            {
                Content = new ByteArrayContent([]),
                RequestMessage = request,
            };
            response.Headers.TryAddWithoutValidation("ETag", "\"clock-etag\"");
            response.Headers.TryAddWithoutValidation("Last-Modified", "Wed, 07 Jun 2045 08:09:10 GMT");
            response.Headers.TryAddWithoutValidation("x-ms-request-id", "clock-request");
            response.Headers.TryAddWithoutValidation("x-ms-version", "2025-11-05");
            return response;
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        bool IsMetadata,
        string? ValidUntilUtc,
        CancellationToken CancellationToken);
}
