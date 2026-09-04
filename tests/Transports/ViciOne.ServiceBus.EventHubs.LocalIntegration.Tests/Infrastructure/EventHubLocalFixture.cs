using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;

internal sealed class EventHubLocalFixture : IAsyncDisposable
{
    public const string ConsumerGroup = "cg1";
    private const string EmulatorKeyName = "RootManageSharedAccessKey";
    private const string EmulatorKey = "SAS_KEY_VALUE";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerPrefix;

    private EventHubLocalFixture(
        string eventHubConnectionString,
        string storageConnectionString,
        TimeSpan operationTimeout,
        string containerPrefix)
    {
        EventHubConnectionString = eventHubConnectionString;
        StorageConnectionString = storageConnectionString;
        OperationTimeout = operationTimeout;
        _containerPrefix = containerPrefix;
        _blobServiceClient = new BlobServiceClient(storageConnectionString);
    }

    public string EventHubConnectionString { get; }

    public string StorageConnectionString { get; }

    public TimeSpan OperationTimeout { get; }

    public static EventHubLocalFixture Create(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.EventHubs);
        EventHubsLocalOptions eventHubs = options.LocalInfrastructure!.EventHubs!;
        string eventHubConnectionString =
            $"Endpoint=sb://{eventHubs.Host}:{eventHubs.Port};" +
            $"SharedAccessKeyName={EmulatorKeyName};SharedAccessKey={EmulatorKey};" +
            "UseDevelopmentEmulator=true;";
        string storageConnectionString =
            $"DefaultEndpointsProtocol=http;AccountName={eventHubs.StorageAccountName};" +
            $"AccountKey={eventHubs.StorageAccountKey};" +
            $"BlobEndpoint=http://{eventHubs.StorageHost}:{eventHubs.StoragePort}/{eventHubs.StorageAccountName};";

        return new EventHubLocalFixture(
            eventHubConnectionString,
            storageConnectionString,
            options.OperationTimeout!.Value,
            CreateContainerPrefix(purpose, Guid.NewGuid()));
    }

    internal static string CreateContainerPrefix(string purpose, Guid runId)
    {
        string normalized = new(purpose.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (normalized.Length == 0)
            normalized = "test";
        if (normalized.Length > 16)
            normalized = normalized[..16];

        return $"vsb-{normalized}-{runId:N}";
    }

    public string ContainerName(string purpose)
    {
        string suffix = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            .ToArray());
        if (suffix.Length == 0)
            throw new ArgumentException("The container purpose must contain an Azure name character.", nameof(purpose));

        string name = $"{_containerPrefix}-{suffix}";
        return name.Length <= 63 ? name : name[..63].TrimEnd('-');
    }

    public void Configure(
        IEventHubFactoryConfigurator configurator,
        Action<BlobClientOptions>? configureStorage = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.Host(EventHubConnectionString);
        configurator.Storage(StorageConnectionString, configureStorage);
    }

    public EventHubProducerClient CreateRawProducer(string eventHubName) =>
        new(EventHubConnectionString, eventHubName, new EventHubProducerClientOptions
        {
            RetryOptions = { MaximumRetries = 0 },
        });

    public BlobContainerClient GetContainer(string name) => _blobServiceClient.GetBlobContainerClient(name);

    public async ValueTask DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(OperationTimeout);
        await foreach (BlobContainerItem container in _blobServiceClient.GetBlobContainersAsync(
                           prefix: _containerPrefix,
                           cancellationToken: cleanup.Token))
        {
            await _blobServiceClient.DeleteBlobContainerAsync(container.Name, cancellationToken: cleanup.Token);
        }
    }
}
