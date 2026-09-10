using Azure.Storage;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.Azure.Storage.LocalIntegration.Tests.Infrastructure;

internal sealed class AzureBlobTestContainer : IAsyncDisposable
{
    private AzureBlobTestContainer(BlobContainerClient container, TimeSpan operationTimeout)
    {
        Container = container;
        OperationTimeout = operationTimeout;
    }

    public BlobContainerClient Container { get; }

    public TimeSpan OperationTimeout { get; }

    public static AzureBlobTestContainer Create(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureBlob);
        AzureBlobLocalOptions azureBlob = options.LocalInfrastructure!.AzureBlob!;
        var endpoint = new Uri($"http://{azureBlob.Host}:{azureBlob.Port}/{azureBlob.AccountName}");
        var credential = new StorageSharedKeyCredential(azureBlob.AccountName, azureBlob.AccountKey);
        var serviceClient = new BlobServiceClient(endpoint, credential);
        string prefix = new(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (prefix.Length == 0)
            prefix = "test";
        if (prefix.Length > 20)
            prefix = prefix[..20];

        string containerName = $"vicione-{prefix}-{Guid.NewGuid():N}";
        return new AzureBlobTestContainer(
            serviceClient.GetBlobContainerClient(containerName),
            options.OperationTimeout!.Value);
    }

    public async ValueTask DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(OperationTimeout);
        await Container.DeleteIfExistsAsync(cancellationToken: cleanup.Token);
    }
}
