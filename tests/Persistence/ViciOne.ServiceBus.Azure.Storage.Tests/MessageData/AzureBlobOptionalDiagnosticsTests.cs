using System.IO.Compression;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Xml.Linq;
using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Storage.MessageData;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Storage.Tests.MessageData;

public sealed class AzureBlobOptionalDiagnosticsTests
{
    private static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("public Azure SDK optional diagnostic payload 330");
    private static readonly Uri Address = new("https://account.blob.core.windows.net/message-data/diagnostic-payload");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-DOWNLOAD", "optional-debug-does-not-replace-download-outcome")]
    public Task GetAsync_OptionalDiagnosticsPreservePublicDownloadOutcomeAsync(int mode) =>
        ObserveOperationAsync(0, mode);

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-AZURE-STORAGE-UPLOAD", "optional-debug-does-not-replace-committed-upload-outcome")]
    public Task PutAsync_OptionalDiagnosticsPreservePublicUploadOutcomeAsync(bool gzip, int mode) =>
        ObserveOperationAsync(gzip ? 2 : 1, mode);

    private static async Task ObserveOperationAsync(int route, int mode)
    {
        ILogContext? previous = LogContext.Current;
        var logger = new SelectedDebugLogger(route == 0 ? "GET" : "PUT", mode);
        using var handler = new HeldBlobHandler(route, mode == 3);
        using var input = new MemoryStream(Payload, writable: false);
        using var caller = new CancellationTokenSource();
        Task? operation = null;
        Task<Stream>? download = null;
        Task<Uri>? upload = null;
        Stream? returned = null;
        bool operationObserved = false;
        Exception? primary = null;
        List<Exception> cleanupFailures = [];
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            logger.Armed = true;
            var options = new BlobClientOptions { Transport = new HttpClientTransport(handler) };
            options.Retry.MaxRetries = 0;
            var container = new BlobContainerClient(
                new Uri("https://account.blob.core.windows.net/message-data?sv=2024-11-04&sr=c&sp=rw&sig=Signature330"),
                options);
            var repository = new AzureBlobMessageDataRepository(container, new FixedBlobNameGenerator(), route == 2);
            Exception? invocationFailure = Record.Exception(() =>
            {
                if (route == 0)
                {
                    download = repository.GetAsync(Address, caller.Token);
                    operation = download;
                }
                else
                {
                    upload = repository.PutAsync(input, cancellationToken: caller.Token);
                    operation = upload;
                }
            });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            await Task.WhenAny(operation, handler.FinalEntered.Task).WaitAsync(WaitBound, CancellationToken.None);

            // A pre-admission diagnostic fault is observed as a real public Task outcome before the counter oracle.
            if (route == 0 && (mode is 1 or 2) && operation.IsCompleted)
            {
                Exception? early = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
                operationObserved = operation.IsCompleted;
                Assert.True(operation.IsFaulted);
                Assert.Same(logger.DiagnosticFailure, early);
                AssertDiagnostic(logger, route, mode);
            }
            Assert.Equal(route == 2 ? 2 : 1, handler.Requests.Length);
            Assert.True(handler.FinalEntered.Task.IsCompletedSuccessfully);
            Assert.False(operation.IsCompleted);
            RecordedRequest final = handler.Requests[^1];
            Assert.Equal(Address.AbsolutePath, final.Uri.AbsolutePath);
            Assert.Contains("sig=Signature330", final.Uri.Query, StringComparison.Ordinal);
            Assert.True(final.Token.CanBeCanceled);
            Assert.False(final.Token.IsCancellationRequested);
            Assert.Equal(route == 0 ? HttpMethod.Get : HttpMethod.Put, final.Method);
            if (route == 1)
            {
                Assert.Equal(Payload, final.Body);
                Assert.Equal("*", final.IfNoneMatch);
            }
            if (route == 2)
            {
                RecordedRequest stage = handler.Requests[0];
                Assert.Contains("comp=block&", stage.Uri.Query, StringComparison.Ordinal);
                Assert.Contains("comp=blocklist", final.Uri.Query, StringComparison.Ordinal);
                Assert.Equal("gzip", final.ContentEncoding);
                Assert.Equal("*", final.IfNoneMatch);
                using var compressed = new MemoryStream(stage.Body, writable: false);
                using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
                using var decoded = new MemoryStream();
                await gzip.CopyToAsync(decoded, CancellationToken.None);
                Assert.Equal(Payload, decoded.ToArray());
                using var blockListStream = new MemoryStream(final.Body, writable: false);
                XDocument blockList = XDocument.Load(blockListStream);
                string encodedBlockId = Assert.Single(stage.Uri.Query.TrimStart('?').Split('&'),
                    part => part.StartsWith("blockid=", StringComparison.Ordinal));
                Assert.Equal(Uri.UnescapeDataString(encodedBlockId[8..]), Assert.Single(blockList.Root!.Elements()).Value);
            }
            handler.ReleaseFinal.TrySetResult();
            Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            operationObserved = operation.IsCompleted;
            foreach (Task raw in handler.SendTasks)
                await raw.WaitAsync(WaitBound, CancellationToken.None);

            if (mode == 3)
            {
                RequestFailedException sdk = route == 0
                    ? Assert.IsType<RequestFailedException>(Assert.IsType<MessageDataException>(failure).InnerException)
                    : Assert.IsType<RequestFailedException>(failure);
                Assert.Equal(400, sdk.Status);
                Assert.Equal("InvalidHeaderValue", sdk.ErrorCode);
                Assert.Equal(route == 0 ? 1 : 0, logger.DebugProbes);
                Assert.Equal(route == 0 ? 1 : 0, logger.Entries.Count);
                if (route == 0)
                    AssertDiagnostic(logger, route, 0);
            }
            else
            {
                AssertDiagnostic(logger, route, mode);
                if ((mode is 1 or 2) && failure is not null)
                    Assert.Same(logger.DiagnosticFailure, failure);
                Assert.Null(failure);
                if (route == 0)
                {
                    returned = await download!.WaitAsync(WaitBound, CancellationToken.None);
                    using var bytes = new MemoryStream();
                    await returned.CopyToAsync(bytes, CancellationToken.None);
                    Assert.Equal(Payload, bytes.ToArray());
                }
                else
                {
                    Uri committed = await upload!.WaitAsync(WaitBound, CancellationToken.None);
                    Assert.Equal(Address, committed);
                    Assert.Empty(committed.Query);
                }
            }
            Assert.True(input.CanRead);
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            logger.Armed = false;
            handler.ReleaseFinal.TrySetResult();
            try
            {
                if (operation is not null && !operationObserved)
                {
                    try { await operation.WaitAsync(WaitBound, CancellationToken.None); }
                    catch (Exception exception)
                    {
                        bool expectedDiagnostic = ReferenceEquals(exception, logger.DiagnosticFailure);
                        bool expectedBackend = mode == 3 && (exception is RequestFailedException { Status: 400 }
                            || exception is MessageDataException { InnerException: RequestFailedException { Status: 400 } });
                        if (!expectedDiagnostic && !expectedBackend)
                            cleanupFailures.Add(exception);
                    }
                }
                foreach (Task raw in handler.SendTasks)
                {
                    try { await raw.WaitAsync(WaitBound, CancellationToken.None); }
                    catch (Exception exception) { cleanupFailures.Add(exception); }
                }
                if (returned is null && download?.IsCompletedSuccessfully == true)
                    returned = download!.Result;
                if (returned is not null)
                {
                    try { await returned.DisposeAsync().AsTask().WaitAsync(WaitBound, CancellationToken.None); }
                    catch (Exception exception) { cleanupFailures.Add(exception); }
                }
                if (!input.CanRead)
                    cleanupFailures.Add(new InvalidOperationException("The repository disposed its borrowed input stream."));
            }
            finally
            {
                LogContext.Current = previous;
            }
            if (cleanupFailures.Count > 0)
            {
                if (primary is not null)
                    cleanupFailures.Insert(0, primary);
                if (cleanupFailures.Count == 1)
                    ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
                throw new AggregateException(cleanupFailures);
            }
        }
    }

    private static void AssertDiagnostic(SelectedDebugLogger logger, int route, int mode)
    {
        Assert.Equal(1, logger.DebugProbes);
        if (mode == 1)
            Assert.Empty(logger.Entries);
        else
        {
            LogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(route == 0 ? "GET Message Data: {Address} ({Blob})" : "PUT Message Data: {Address} ({Blob})", entry.Template);
            Assert.Equal(Address, Assert.IsType<Uri>(entry.Address));
            Assert.Empty(((Uri)entry.Address!).Query);
            Assert.Equal("diagnostic-payload", entry.Blob);
            Assert.Null(entry.Exception);
        }
    }

    private sealed class FixedBlobNameGenerator : IBlobNameGenerator
    {
        public string GenerateBlobName() => "diagnostic-payload";
    }

    private sealed class SelectedDebugLogger(string verb, int mode) : ILogger
    {
        public bool Armed { get; set; }
        public IOException DiagnosticFailure { get; } = new("unique selected Azure Blob diagnostic failure");
        public int DebugProbes { get; private set; }
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel != LogLevel.Debug || !Armed)
                return false;
            DebugProbes++;
            if (mode == 1)
                throw DiagnosticFailure;
            return true;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Debug || !Armed)
                return;
            var values = state as IEnumerable<KeyValuePair<string, object?>>
                ?? throw new InvalidOperationException("Structured diagnostic state expected.");
            Dictionary<string, object?> fields = values.ToDictionary(item => item.Key, item => item.Value);
            string? template = fields["{OriginalFormat}"] as string;
            if (template != verb + " Message Data: {Address} ({Blob})")
                throw new InvalidOperationException("Unexpected debug template reached the selected logger.");
            Entries.Add(new LogEntry(template, fields["Address"], fields["Blob"], exception));
            if (mode == 2)
                throw DiagnosticFailure;
        }
    }

    private sealed record LogEntry(string? Template, object? Address, object? Blob, Exception? Exception);
    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, byte[] Body,
        string? IfNoneMatch, string? ContentEncoding, CancellationToken Token);

    private sealed class HeldBlobHandler(int route, bool backendFailure) : HttpMessageHandler
    {
        private readonly object _sync = new();
        private readonly List<Task> _sendTasks = [];
        private readonly List<RecordedRequest> _requests = [];
        public TaskCompletionSource FinalEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFinal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task[] SendTasks { get { lock (_sync) return [.. _sendTasks]; } }
        public RecordedRequest[] Requests { get { lock (_sync) return [.. _requests]; } }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Task<HttpResponseMessage> actual = SendCoreAsync(request, cancellationToken);
            lock (_sync) _sendTasks.Add(actual);
            return actual;
        }

        private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Uri uri = request.RequestUri ?? throw new InvalidOperationException("A real request URI is required.");
            byte[] body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            bool stage = uri.Query.Contains("comp=block&", StringComparison.Ordinal);
            bool commit = uri.Query.Contains("comp=blocklist", StringComparison.Ordinal);
            bool final = route == 0 ? request.Method == HttpMethod.Get
                : route == 1 ? request.Method == HttpMethod.Put && !stage && !commit
                : request.Method == HttpMethod.Put && commit;
            if ((!final && !(route == 2 && request.Method == HttpMethod.Put && stage))
                || uri.AbsolutePath != Address.AbsolutePath)
                throw new InvalidOperationException("Unexpected HTTP operation at the controlled Azure SDK boundary.");
            string? condition = request.Headers.TryGetValues("If-None-Match", out IEnumerable<string>? conditions)
                ? string.Join(",", conditions) : null;
            string? encoding = request.Headers.TryGetValues("x-ms-blob-content-encoding", out IEnumerable<string>? encodings)
                ? string.Join(",", encodings) : null;
            lock (_sync) _requests.Add(new RecordedRequest(request.Method, uri, body, condition, encoding, cancellationToken));
            if (final)
            {
                FinalEntered.TrySetResult();
                await ReleaseFinal.Task.WaitAsync(cancellationToken);
            }
            bool fail = final && backendFailure;
            byte[] responseBody = fail
                ? Encoding.UTF8.GetBytes("<Error><Code>InvalidHeaderValue</Code><Message>controlled actual backend 400</Message></Error>")
                : route == 0 ? Payload : [];
            var response = new HttpResponseMessage(fail ? HttpStatusCode.BadRequest : route == 0 ? HttpStatusCode.OK : HttpStatusCode.Created)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(responseBody),
            };
            response.Headers.TryAddWithoutValidation("ETag", "\"diagnostic-etag\"");
            response.Headers.TryAddWithoutValidation("x-ms-request-id", "diagnostic-request-330");
            response.Headers.TryAddWithoutValidation("x-ms-version", "2025-11-05");
            response.Content.Headers.LastModified = new DateTimeOffset(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);
            if (fail)
            {
                response.Headers.TryAddWithoutValidation("x-ms-error-code", "InvalidHeaderValue");
                response.Content.Headers.ContentType = new("application/xml");
            }
            return response;
        }
    }
}
