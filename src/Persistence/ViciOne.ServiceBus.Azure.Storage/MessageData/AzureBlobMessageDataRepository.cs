using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Azure.Storage.MessageData;

/// <summary>Stores message payloads as blobs in a caller-owned Azure Blob Storage container.</summary>
public sealed class AzureBlobMessageDataRepository :
    IMessageDataRepository,
    IBusObserver
{
    private const string CompressedBlobSuffix = ".gz";
    private const string ExpirationMetadataName = "ValidUntilUtc";

    private readonly BlobContainerClient _containerClient;
    private readonly IBlobNameGenerator _blobNameGenerator;
    private readonly bool _compress;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates a repository that generates sequential blob names.</summary>
    /// <param name="containerClient">The caller-owned container client used for every storage operation.</param>
    /// <param name="compress">Whether new payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The clock used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureBlobMessageDataRepository(
        BlobContainerClient containerClient,
        bool compress = false,
        TimeProvider? timeProvider = null)
        : this(containerClient, new NewIdBlobNameGenerator(), compress, timeProvider)
    {
    }

    /// <summary>Creates a repository that uses a caller-supplied blob-name strategy.</summary>
    /// <param name="containerClient">The caller-owned container client used for every storage operation.</param>
    /// <param name="blobNameGenerator">The strategy that assigns a unique name to each uploaded blob.</param>
    /// <param name="compress">Whether new payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The clock used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureBlobMessageDataRepository(
        BlobContainerClient containerClient,
        IBlobNameGenerator blobNameGenerator,
        bool compress = false,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(containerClient);
        ArgumentNullException.ThrowIfNull(blobNameGenerator);
        if (string.IsNullOrWhiteSpace(containerClient.Name))
        {
            throw new ArgumentException(
                "The Azure Blob Storage client must identify a container.",
                nameof(containerClient));
        }

        _containerClient = containerClient;
        _blobNameGenerator = blobNameGenerator;
        _compress = compress;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Observes successful bus creation; this repository requires no post-creation work.</summary>
    /// <param name="bus">The bus that was created.</param>
    public void PostCreate(IBus bus) => ArgumentNullException.ThrowIfNull(bus);

    /// <summary>Observes a bus-creation failure; this repository performs no recovery work.</summary>
    /// <param name="exception">The exception that prevented bus creation.</param>
    public void CreateFaulted(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

    /// <summary>Ensures that the configured container exists before the bus starts.</summary>
    /// <param name="bus">The bus that is about to start.</param>
    /// <returns>A task that completes after the best-effort container initialization.</returns>
    public async Task PreStartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        try
        {
            global::Azure.Response<bool> containerExists = await _containerClient.ExistsAsync().ConfigureAwait(false);
            if (!containerExists.Value)
                await _containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
        catch (RequestFailedException exception)
        {
            LogContext.Warning?.Log(
                exception,
                "Azure Blob Storage container initialization failed: {Address}",
                _containerClient.Uri);
        }
    }

    /// <summary>Observes successful bus startup; this repository requires no post-start work.</summary>
    /// <param name="bus">The bus that started.</param>
    /// <param name="busReady">The task that reports the bus readiness result.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-start failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose start failed.</param>
    /// <param name="exception">The startup exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>Observes that the bus is about to stop; this repository owns no shutdown work.</summary>
    /// <param name="bus">The bus that is about to stop.</param>
    /// <returns>An already-completed task.</returns>
    public Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>Observes successful bus shutdown; this repository owns no post-stop work.</summary>
    /// <param name="bus">The bus that stopped.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-stop failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose stop failed.</param>
    /// <param name="exception">The shutdown exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>Opens the blob identified by an address previously returned by this repository.</summary>
    /// <param name="address">The absolute URI of a blob in the configured container.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the readable, transparently decompressed payload stream.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = ResolveBlobClient(address);

        try
        {
            LogContext.Debug?.Log("GET Message Data: {Address} ({Blob})", address, blobClient.Name);

            global::Azure.Response<BlobDownloadStreamingResult> response = await blobClient
                .DownloadStreamingAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            Stream stream = response.Value.Content;

            return blobClient.Name.EndsWith(CompressedBlobSuffix, StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false)
                : stream;
        }
        catch (RequestFailedException exception)
        {
            throw new MessageDataException(
                $"Message-data content could not be retrieved: {blobClient.BlobContainerName}/{blobClient.Name}",
                exception);
        }
    }

    /// <summary>Uploads a readable payload under a generated blob name and optionally records its expiration.</summary>
    /// <param name="stream">The readable payload stream to upload.</param>
    /// <param name="timeToLive">An optional relative lifetime stored as UTC expiration metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the URI of the uploaded blob.</returns>
    public async Task<Uri> PutAsync(
        Stream stream,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("The message-data stream must be readable.", nameof(stream));

        string blobName = _blobNameGenerator.GenerateBlobName();
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new InvalidOperationException(
                "The configured blob-name generator returned an empty blob name.");
        }

        if (_compress)
            blobName += CompressedBlobSuffix;

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);

        if (_compress)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
                await stream.CopyToAsync(gzipStream, cancellationToken).ConfigureAwait(false);

            compressedStream.Position = 0;
            await blobClient.UploadAsync(compressedStream, cancellationToken).ConfigureAwait(false);
        }
        else
            await blobClient.UploadAsync(stream, cancellationToken).ConfigureAwait(false);

        await SetBlobExpirationAsync(blobClient, timeToLive, _timeProvider, cancellationToken)
            .ConfigureAwait(false);

        LogContext.Debug?.Log("PUT Message Data: {Address} ({Blob})", blobClient.Uri, blobClient.Name);

        return blobClient.Uri;
    }

    private BlobClient ResolveBlobClient(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri ||
            !string.Equals(address.Scheme, _containerClient.Uri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Authority, _containerClient.Uri.Authority, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The message-data address must be an absolute blob URI for the configured storage endpoint.",
                nameof(address));
        }

        var addressBuilder = new BlobUriBuilder(address);
        if (!string.Equals(
                addressBuilder.BlobContainerName,
                _containerClient.Name,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(addressBuilder.BlobName))
        {
            throw new ArgumentException(
                "The message-data address must identify a blob in the configured container.",
                nameof(address));
        }

        BlobClient blobClient = _containerClient.GetBlobClient(addressBuilder.BlobName);
        if (!string.Equals(
                blobClient.Uri.AbsolutePath,
                address.AbsolutePath,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The message-data address must identify a blob in the configured container.",
                nameof(address));
        }

        return blobClient;
    }

    private static async Task SetBlobExpirationAsync(
        BlobBaseClient blobClient,
        TimeSpan? timeToLive,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (timeToLive is null)
            return;

        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset expiration = utcNow + timeToLive.Value;
        if (expiration <= utcNow)
            expiration = utcNow + TimeSpan.FromMinutes(1);

        var metadata = new Dictionary<string, string>
        {
            [ExpirationMetadataName] = expiration.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        };

        await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
