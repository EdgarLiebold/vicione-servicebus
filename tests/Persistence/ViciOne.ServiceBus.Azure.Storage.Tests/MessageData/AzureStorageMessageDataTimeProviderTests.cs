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

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "ttl-metadata-is-atomic-and-uses-injected-clock")]
    public async Task TimeToLiveMetadata_IsWrittenAtomicallyUsingTheInjectedClockAsync()
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));

        Uri address = await repository.PutAsync(
            new MemoryStream([1, 2, 3]),
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        Assert.StartsWith("https://clock.blob.core.windows.net/message-data/", address.AbsoluteUri, StringComparison.Ordinal);
        RecordedRequest upload = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, upload.Method);
        Assert.Equal((Now.UtcDateTime + TimeSpan.FromMinutes(5)).ToString("O"), upload.ValidUntilUtc);
        Assert.True(upload.CancellationToken.CanBeCanceled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "nonpositive-ttl-is-rejected-before-upload")]
    public async Task NonPositiveTimeToLive_IsRejectedBeforeUploadAsync(int requestedMinutes)
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.PutAsync(
                new MemoryStream([1, 2, 3]),
                TimeSpan.FromMinutes(requestedMinutes),
                TestContext.Current.CancellationToken));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "overflowing-expiration-is-rejected-before-upload")]
    public async Task ExpirationOutsideDateTimeOffsetRange_IsRejectedBeforeUploadAsync()
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(DateTimeOffset.MaxValue.AddMinutes(-1)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.PutAsync(
                new MemoryStream([1, 2, 3]),
                TimeSpan.FromMinutes(5),
                TestContext.Current.CancellationToken));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "missing-ttl-does-not-write-expiration-metadata")]
    public async Task MissingTimeToLive_DoesNotIssueAMetadataWriteAsync()
    {
        var handler = new RecordingBlobHandler();
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));

        await repository.PutAsync(new MemoryStream([4, 5, 6]), cancellationToken: TestContext.Current.CancellationToken);

        RecordedRequest upload = Assert.Single(handler.Requests);
        Assert.Null(upload.ValidUntilUtc);
        Assert.True(upload.CancellationToken.CanBeCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME", "atomic-ttl-upload-honors-cancellation")]
    public async Task TimeToLiveUpload_HonorsCallerCancellationAsync()
    {
        var handler = new RecordingBlobHandler { BlockUpload = true };
        var repository = CreateRepository(handler, new FixedTimeProvider(Now));
        using var cancellation = new CancellationTokenSource();

        Task<Uri> put = repository.PutAsync(
            new MemoryStream([7, 8, 9]),
            TimeSpan.FromMinutes(5),
            cancellation.Token);
        await handler.UploadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        Task completed = await Task.WhenAny(
            put,
            Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
        handler.ReleaseUpload();

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
        private readonly TaskCompletionSource _uploadRelease = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockUpload { get; init; }

        public TaskCompletionSource UploadStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public List<RecordedRequest> Requests { get; } = [];

        public void ReleaseUpload() => _uploadRelease.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            string? validUntilUtc = request.Headers.TryGetValues("x-ms-meta-ValidUntilUtc", out IEnumerable<string>? values)
                ? Assert.Single(values)
                : null;
            Requests.Add(new RecordedRequest(request.Method, validUntilUtc, cancellationToken));

            if (validUntilUtc is not null && BlockUpload)
            {
                UploadStarted.TrySetResult();
                await _uploadRelease.Task.WaitAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(HttpStatusCode.Created)
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
        string? ValidUntilUtc,
        CancellationToken CancellationToken);
}
