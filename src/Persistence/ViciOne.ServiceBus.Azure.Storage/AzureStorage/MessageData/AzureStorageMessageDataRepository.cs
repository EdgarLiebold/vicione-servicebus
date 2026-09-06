using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace ViciOne.ServiceBus.AzureStorage.MessageData;

/// <summary>Stores message payloads as blobs in one Azure Blob Storage container.</summary>
public class AzureStorageMessageDataRepository :
    IMessageDataRepository,
    IBusObserver
{
    readonly BlobContainerClient _container;
    readonly IBlobNameGenerator _nameGenerator;
    readonly bool _compress;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a repository from an Azure Storage connection string.</summary>
    /// <param name="connectionString">The connection string used to create the blob service client.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(string connectionString, string containerName, bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(connectionString), containerName, compress, timeProvider)
    {
    }

    /// <summary>Creates a repository that authenticates to Azure Blob Storage with a shared account key.</summary>
    /// <param name="serviceUri">The blob service endpoint.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="accountName">The Azure Storage account name.</param>
    /// <param name="accountKey">The Azure Storage account key.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string accountName, string accountKey, bool compress = false,
        TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new StorageSharedKeyCredential(accountName, accountKey)), containerName, compress, timeProvider)
    {
    }

    /// <summary>Creates a repository that authenticates to Azure Blob Storage with a shared access signature.</summary>
    /// <param name="serviceUri">The blob service endpoint.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="signature">The shared access signature used by the blob service client.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string signature, bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new AzureSasCredential(signature)), containerName, compress, timeProvider)
    {
    }

    /// <summary>Creates a repository that authenticates to Azure Blob Storage with a Microsoft Entra application credential.</summary>
    /// <param name="serviceUri">The blob service endpoint.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="tenantId">The external Microsoft Entra tenant that issued the application identity.</param>
    /// <param name="clientId">The application registration identifier.</param>
    /// <param name="clientSecret">The application credential used to authenticate the client.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string tenantId, string clientId, string clientSecret,
        bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new ClientSecretCredential(tenantId, clientId, clientSecret)), containerName, compress, timeProvider)
    {
    }

    /// <summary>Creates a repository with caller-owned client lifetime and sequentially generated blob names.</summary>
    /// <param name="client">The caller-owned blob service client.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(BlobServiceClient client, string containerName, bool compress = false, TimeProvider? timeProvider = null)
        : this(client, containerName, new NewIdBlobNameGenerator(), compress, timeProvider)
    {
    }

    /// <summary>Creates a repository with caller-owned client lifetime and blob-name generation.</summary>
    /// <param name="client">The caller-owned blob service client.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="nameGenerator">The strategy that assigns a unique name to each uploaded blob.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The time source used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    public AzureStorageMessageDataRepository(BlobServiceClient client, string containerName, IBlobNameGenerator nameGenerator, bool compress = false,
        TimeProvider? timeProvider = null)
    {
        _container = client.GetBlobContainerClient(containerName);
        _nameGenerator = nameGenerator;
        _compress = compress;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Observes successful bus creation; this repository requires no post-creation work.</summary>
    /// <param name="bus">The bus that was created.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>Observes a bus-creation failure; this repository performs no recovery work.</summary>
    /// <param name="exception">The exception that prevented bus creation.</param>
    public void CreateFaulted(Exception exception)
    {
    }

    /// <summary>Checks for the configured container and attempts to create it before the bus starts.</summary>
    /// <param name="bus">The bus that is about to start.</param>
    /// <returns>A task that completes after the best-effort container check; Azure failures are logged and do not fail bus startup.</returns>
    public async Task PreStartAsync(IBus bus)
    {
        try
        {
            global::Azure.Response<bool> containerExists = await _container.ExistsAsync().ConfigureAwait(false);
            if (!containerExists.Value)
            {
                try
                {
                    await _container.CreateIfNotExistsAsync().ConfigureAwait(false);
                }
                catch (RequestFailedException exception)
                {
                    LogContext.Warning?.Log(exception, "Azure Storage Container does not exist: {Address}", _container.Uri);
                }
            }
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Azure Storage failure.");
        }
    }

    /// <summary>Observes successful bus startup; this repository requires no post-start work.</summary>
    /// <param name="bus">The bus that started.</param>
    /// <param name="busReady">The task that reports the bus readiness result.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-start failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose start failed.</param>
    /// <param name="exception">The startup exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>Observes that the bus is about to stop; this repository owns no shutdown work.</summary>
    /// <param name="bus">The bus that is about to stop.</param>
    /// <returns>An already-completed task.</returns>
    public Task PreStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Observes successful bus shutdown; this repository owns no post-stop work.</summary>
    /// <param name="bus">The bus that stopped.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-stop failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose stop failed.</param>
    /// <param name="exception">The shutdown exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>Opens the blob identified by the supplied address and exposes its uncompressed payload stream.</summary>
    /// <param name="address">The blob URI whose blob name is resolved within this repository's configured container.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the readable payload stream.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var blobName = new BlobUriBuilder(address).BlobName;
        var blob = _container.GetBlobClient(blobName);
        try
        {
            LogContext.Debug?.Log("GET Message Data: {Address} ({Blob})", address, blob.Name);

            var stream = await blob.OpenReadAsync(new BlobOpenReadOptions(false), cancellationToken).ConfigureAwait(false);
            return blobName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) || _compress ? new GZipStream(stream, CompressionMode.Decompress, false) : stream;
        }
        catch (RequestFailedException exception)
        {
            throw new MessageDataException($"MessageData content not found: {blob.BlobContainerName}/{blob.Name}", exception);
        }
    }

    /// <summary>Uploads a payload under a generated blob name and optionally records its expiration metadata.</summary>
    /// <param name="stream">The payload stream to upload.</param>
    /// <param name="timeToLive">An optional relative lifetime stored as <c>ValidUntilUtc</c> blob metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the URI of the uploaded blob.</returns>
    public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
    {
        var blobName = _nameGenerator.GenerateBlobName();
        if (_compress)
        {
            blobName += ".gz";
        }

        var blob = _container.GetBlobClient(blobName);

        if (_compress)
        {
            var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
            {
                await stream.CopyToAsync(gzipStream);
            }

            compressedStream.Position = 0;
            await blob.UploadAsync(compressedStream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await blob.UploadAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        await SetBlobExpirationAsync(blob, timeToLive, _timeProvider).ConfigureAwait(false);

        LogContext.Debug?.Log("PUT Message Data: {Address} ({Blob})", blob.Uri, blob.Name);

        return blob.Uri;
    }

    static async Task SetBlobExpirationAsync(BlobBaseClient blob, TimeSpan? timeToLive, TimeProvider timeProvider)
    {
        if (timeToLive.HasValue)
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;

            var expirationDate = utcNow + timeToLive.Value;
            if (expirationDate <= utcNow)
                expirationDate = utcNow + TimeSpan.FromMinutes(1);

            var metadata = new Dictionary<string, string> { { "ValidUntilUtc", expirationDate.ToString("O") } };
            await blob.SetMetadataAsync(metadata).ConfigureAwait(false);
        }
    }
}
