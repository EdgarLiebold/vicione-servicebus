using System.IO.Compression;
using System.Net;
using System.Reflection;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Azure.Storage.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Storage.Tests.MessageData;

public sealed class AzureBlobMessageDataRepositoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-UPLOAD", "plain-payload-address-and-cancellation")]
    public async Task PutAsync_UploadsPlainPayloadAndForwardsCancellationAsync()
    {
        byte[] payload = [1, 3, 5, 7, 9];
        var handler = new RecordingBlobHandler();
        AzureBlobMessageDataRepository repository = CreateRepository(handler, "plain-payload");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Uri address = await repository.PutAsync(
            new MemoryStream(payload),
            cancellationToken: cancellationToken);

        Assert.Equal(
            "https://account.blob.core.windows.net/message-data/plain-payload",
            address.AbsoluteUri);
        RecordedRequest upload = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, upload.Method);
        Assert.Equal(payload, upload.Body);
        Assert.True(upload.CancellationToken.CanBeCanceled);
        Assert.False(upload.IsMetadata);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-UPLOAD", "compressed-payload-address-and-cancellation")]
    public async Task PutAsync_CompressesPayloadAndForwardsCancellationAsync()
    {
        byte[] payload = [.. Enumerable.Range(0, 256).Select(value => (byte)value)];
        var handler = new RecordingBlobHandler();
        AzureBlobMessageDataRepository repository = CreateRepository(
            handler,
            "compressed-payload",
            compress: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Uri address = await repository.PutAsync(
            new MemoryStream(payload),
            cancellationToken: cancellationToken);

        Assert.Equal(
            "https://account.blob.core.windows.net/message-data/compressed-payload.gz",
            address.AbsoluteUri);
        RecordedRequest upload = Assert.Single(handler.Requests);
        Assert.True(upload.CancellationToken.CanBeCanceled);
        Assert.Equal(payload, Decompress(upload.Body));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-UPLOAD", "stream-and-generated-name-validation")]
    public async Task PutAsync_RejectsInvalidStreamsAndGeneratedNamesBeforeTransportUseAsync()
    {
        var handler = new RecordingBlobHandler();
        AzureBlobMessageDataRepository repository = CreateRepository(handler, "valid-name");
        var unreadable = new MemoryStream();
        unreadable.Dispose();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PutAsync(null!, cancellationToken: cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.PutAsync(unreadable, cancellationToken: cancellationToken));

        foreach (string invalidName in new[] { "", "   " })
        {
            AzureBlobMessageDataRepository invalidRepository = CreateRepository(handler, invalidName);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => invalidRepository.PutAsync(
                    new MemoryStream([1]),
                    cancellationToken: cancellationToken));
        }

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.PutAsync(
                new MemoryStream([1]),
                cancellationToken: cancellation.Token));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-DOWNLOAD", "format-is-derived-from-persisted-address")]
    public async Task GetAsync_DerivesCompressionFromThePersistedBlobAddressAsync()
    {
        byte[] payload = [2, 4, 6, 8, 10];

        var plainHandler = new RecordingBlobHandler(payload);
        AzureBlobMessageDataRepository currentlyCompressingRepository = CreateRepository(
            plainHandler,
            "unused",
            compress: true);
        await using Stream plain = await currentlyCompressingRepository.GetAsync(
            new Uri("https://account.blob.core.windows.net/message-data/previously-plain"),
            TestContext.Current.CancellationToken);
        Assert.Equal(payload, await ReadAllBytesAsync(plain));

        byte[] compressedPayload = Compress(payload);
        var compressedHandler = new RecordingBlobHandler(compressedPayload);
        AzureBlobMessageDataRepository currentlyPlainRepository = CreateRepository(
            compressedHandler,
            "unused",
            compress: false);
        await using Stream compressed = await currentlyPlainRepository.GetAsync(
            new Uri("https://account.blob.core.windows.net/message-data/previously-compressed.gz"),
            TestContext.Current.CancellationToken);
        Assert.Equal(payload, await ReadAllBytesAsync(compressed));

        Assert.All(
            plainHandler.Requests.Concat(compressedHandler.Requests),
            request => Assert.True(request.CancellationToken.CanBeCanceled));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-DOWNLOAD", "repository-address-boundary")]
    public async Task GetAsync_RejectsAddressesOutsideTheConfiguredContainerBeforeTransportUseAsync()
    {
        var handler = new RecordingBlobHandler([1]);
        AzureBlobMessageDataRepository repository = CreateRepository(handler, "unused");
        Uri[] invalidAddresses =
        [
            new("relative", UriKind.Relative),
            new("https://foreign.blob.core.windows.net/message-data/blob", UriKind.Absolute),
            new("https://account.blob.core.windows.net/foreign/blob", UriKind.Absolute),
            new("https://account.blob.core.windows.net/message-data", UriKind.Absolute),
        ];

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.GetAsync(null!, cancellationToken));
        foreach (Uri address in invalidAddresses)
            await Assert.ThrowsAsync<ArgumentException>(
                () => repository.GetAsync(address, cancellationToken));

        Assert.Empty(handler.Requests);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAsync(
                new Uri("https://account.blob.core.windows.net/message-data/blob"),
                cancellation.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-DOWNLOAD", "azure-failure-is-translated")]
    public async Task GetAsync_TranslatesAzureRequestFailuresToMessageDataExceptionAsync()
    {
        var handler = new RecordingBlobHandler(forcedFailure: HttpStatusCode.NotFound);
        AzureBlobMessageDataRepository repository = CreateRepository(handler, "unused");

        MessageDataException exception = await Assert.ThrowsAsync<MessageDataException>(
            () => repository.GetAsync(
                new Uri("https://account.blob.core.windows.net/message-data/missing"),
                TestContext.Current.CancellationToken));

        Assert.IsType<global::Azure.RequestFailedException>(exception.InnerException);
        Assert.Contains("message-data/missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-OBSERVER", "container-existence-and-creation")]
    public async Task PreStartAsync_CreatesOnlyAMissingContainerAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        var existingHandler = new RecordingBlobHandler(containerExists: true);
        await CreateRepository(existingHandler, "unused").PreStartAsync(bus);
        RecordedRequest existingCheck = Assert.Single(existingHandler.Requests);
        Assert.Equal(HttpMethod.Get, existingCheck.Method);
        Assert.True(existingCheck.IsContainerRequest);

        var missingHandler = new RecordingBlobHandler(containerExists: false);
        await CreateRepository(missingHandler, "unused").PreStartAsync(bus);
        Assert.Collection(
            missingHandler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.True(request.IsContainerRequest);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.True(request.IsContainerRequest);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-OBSERVER", "lifecycle-validation-and-best-effort-startup")]
    public async Task BusObserverLifecycle_ValidatesArgumentsAndKeepsAzureStartupBestEffortAsync()
    {
        var handler = new RecordingBlobHandler(forcedFailure: HttpStatusCode.ServiceUnavailable);
        AzureBlobMessageDataRepository repository = CreateRepository(handler, "unused");
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var failure = new InvalidOperationException("causal observer failure");

        repository.PostCreate(bus);
        repository.CreateFaulted(failure);
        await repository.PreStartAsync(bus);
        await repository.PostStartAsync(bus, Task.FromResult<BusReady>(null!));
        await repository.StartFaultedAsync(bus, failure);
        await repository.PreStopAsync(bus);
        await repository.PostStopAsync(bus);
        await repository.StopFaultedAsync(bus, failure);

        Assert.Single(handler.Requests);
        Assert.Throws<ArgumentNullException>(() => repository.PostCreate(null!));
        Assert.Throws<ArgumentNullException>(() => repository.CreateFaulted(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStartAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PostStartAsync(null!, Task.FromResult<BusReady>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStartAsync(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StartFaultedAsync(null!, failure));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StartFaultedAsync(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStopAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStopAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StopFaultedAsync(null!, failure));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StopFaultedAsync(bus, null!));
    }

    private static AzureBlobMessageDataRepository CreateRepository(
        HttpMessageHandler handler,
        string blobName,
        bool compress = false)
    {
        var options = new BlobClientOptions
        {
            Transport = new HttpClientTransport(handler),
        };
        options.Retry.MaxRetries = 0;
        var serviceClient = new BlobServiceClient(
            new Uri("https://account.blob.core.windows.net"),
            options);

        return new AzureBlobMessageDataRepository(
            serviceClient.GetBlobContainerClient("message-data"),
            new FixedBlobNameGenerator(blobName),
            compress);
    }

    private static byte[] Compress(byte[] payload)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            gzip.Write(payload);

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var input = new MemoryStream(payload);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, TestContext.Current.CancellationToken);
        return output.ToArray();
    }

    private sealed class FixedBlobNameGenerator(string blobName) : IBlobNameGenerator
    {
        public string GenerateBlobName() => blobName;
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The repository must not invoke the observed bus.");
    }

    private sealed class RecordingBlobHandler(
        byte[]? downloadContent = null,
        bool containerExists = true,
        HttpStatusCode? forcedFailure = null) : HttpMessageHandler
    {
        private readonly byte[] _downloadContent = downloadContent ?? [];

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] body = request.Content is null
                ? []
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            bool isMetadata = request.RequestUri?.Query.Contains("comp=metadata", StringComparison.Ordinal) == true;
            bool isContainerRequest =
                request.RequestUri?.AbsolutePath.TrimEnd('/').Equals("/message-data", StringComparison.Ordinal) == true;
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!,
                body,
                isMetadata,
                isContainerRequest,
                cancellationToken));

            if (forcedFailure is { } failureStatus)
                return CreateResponse(request, failureStatus, []);

            if (isContainerRequest && request.Method != HttpMethod.Put)
            {
                HttpResponseMessage response = CreateResponse(
                    request,
                    containerExists ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                    []);
                if (!containerExists)
                    response.Headers.TryAddWithoutValidation("x-ms-error-code", "ContainerNotFound");

                return response;
            }

            if (isContainerRequest && request.Method == HttpMethod.Put)
                return CreateResponse(request, HttpStatusCode.Created, []);

            if (request.Method == HttpMethod.Head)
                return CreateResponse(request, HttpStatusCode.OK, _downloadContent);

            if (request.Method == HttpMethod.Get)
            {
                HttpResponseMessage response = CreateResponse(
                    request,
                    HttpStatusCode.OK,
                    _downloadContent);
                return response;
            }

            return CreateResponse(
                request,
                isMetadata ? HttpStatusCode.OK : HttpStatusCode.Created,
                []);
        }

        private static HttpResponseMessage CreateResponse(
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            byte[] content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content),
                RequestMessage = request,
            };
            response.Headers.TryAddWithoutValidation("ETag", "\"test-etag\"");
            response.Content.Headers.LastModified = new DateTimeOffset(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);
            response.Headers.TryAddWithoutValidation("x-ms-request-id", "test-request");
            response.Headers.TryAddWithoutValidation("x-ms-version", "2025-11-05");
            return response;
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        byte[] Body,
        bool IsMetadata,
        bool IsContainerRequest,
        CancellationToken CancellationToken);
}
