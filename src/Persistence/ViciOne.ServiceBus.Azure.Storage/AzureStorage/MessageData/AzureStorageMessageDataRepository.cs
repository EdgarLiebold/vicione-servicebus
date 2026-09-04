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

#nullable enable annotations
namespace ViciOne.ServiceBus.AzureStorage.MessageData;

/// <summary>
/// Provides an azure storage message data repository implementation.
/// </summary>
public class AzureStorageMessageDataRepository :
    IMessageDataRepository,
    IBusObserver
{
    readonly BlobContainerClient _container;
    readonly IBlobNameGenerator _nameGenerator;
    readonly bool _compress;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(string connectionString, string containerName, bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(connectionString), containerName, compress, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceUri">The service uri value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="accountName">The account name value.</param>
    /// <param name="accountKey">The account key value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string accountName, string accountKey, bool compress = false,
        TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new StorageSharedKeyCredential(accountName, accountKey)), containerName, compress, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceUri">The service uri value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="signature">The signature value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string signature, bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new AzureSasCredential(signature)), containerName, compress, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceUri">The service uri value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="tenantId">The tenant id value.</param>
    /// <param name="clientId">The client id value.</param>
    /// <param name="clientSecret">The client secret value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(Uri serviceUri, string containerName, string tenantId, string clientId, string clientSecret,
        bool compress = false, TimeProvider? timeProvider = null)
        : this(new BlobServiceClient(serviceUri, new ClientSecretCredential(tenantId, clientId, clientSecret)), containerName, compress, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(BlobServiceClient client, string containerName, bool compress = false, TimeProvider? timeProvider = null)
        : this(client, containerName, new NewIdBlobNameGenerator(), compress, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="nameGenerator">The name generator value.</param>
    /// <param name="compress">The compress value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AzureStorageMessageDataRepository(BlobServiceClient client, string containerName, IBlobNameGenerator nameGenerator, bool compress = false,
        TimeProvider? timeProvider = null)
    {
        _container = client.GetBlobContainerClient(containerName);
        _nameGenerator = nameGenerator;
        _compress = compress;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>
    /// Creates faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
    }

    /// <summary>
    /// Performs the pre start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="busReady">The bus ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the pre stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the put operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
