using global::Azure;
using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;

internal sealed class AzureServiceBusLocalFixture
{
    private AzureServiceBusLocalFixture(
        string dataConnectionString,
        string managementConnectionString,
        TimeSpan operationTimeout,
        string prefix)
    {
        DataConnectionString = dataConnectionString;
        ManagementConnectionString = managementConnectionString;
        OperationTimeout = operationTimeout;
        Prefix = prefix;
    }

    public string DataConnectionString { get; }

    public string ManagementConnectionString { get; }

    public TimeSpan OperationTimeout { get; }

    public string Prefix { get; }

    public static AzureServiceBusLocalFixture Create(string purpose = "capability")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureServiceBus);
        AzureServiceBusLocalOptions serviceBus = options.LocalInfrastructure!.AzureServiceBus!;
        string credential =
            $"SharedAccessKeyName={serviceBus.SharedAccessKeyName};" +
            $"SharedAccessKey={serviceBus.SharedAccessKey};UseDevelopmentEmulator=true;";

        return new AzureServiceBusLocalFixture(
            $"Endpoint=sb://{serviceBus.Host}:{serviceBus.AmqpPort};{credential}",
            $"Endpoint=sb://{serviceBus.Host}:{serviceBus.ManagementPort};{credential}",
            options.OperationTimeout!.Value,
            EntityName(purpose));
    }

    public ServiceBusClient CreateClient() => new(DataConnectionString, new ServiceBusClientOptions
    {
        RetryOptions = { MaxRetries = 0 },
        TransportType = ServiceBusTransportType.AmqpTcp,
    });

    public ServiceBusAdministrationClient CreateAdministrationClient() =>
        new(ManagementConnectionString, new ServiceBusAdministrationClientOptions
        {
            Retry = { MaxRetries = 0 },
        });

    public CancellationTokenSource OperationCancellation() => new(OperationTimeout);

    public string Name(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        string normalized = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        if (normalized.Length == 0)
            throw new ArgumentException("The entity purpose must contain an Azure name character.", nameof(purpose));

        return $"{Prefix}-{normalized}";
    }

    public async Task CleanupAsync(ServiceBusAdministrationClient administrationClient)
    {
        ArgumentNullException.ThrowIfNull(administrationClient);
        using CancellationTokenSource timeout = OperationCancellation();

        await foreach (QueueProperties queue in administrationClient.GetQueuesAsync(timeout.Token))
        {
            if (queue.Name.StartsWith(Prefix, StringComparison.Ordinal))
                await DeleteQueueIfPresentAsync(administrationClient, queue.Name, timeout.Token);
        }

        await foreach (TopicProperties topic in administrationClient.GetTopicsAsync(timeout.Token))
        {
            if (topic.Name.StartsWith(Prefix, StringComparison.Ordinal))
                await DeleteTopicIfPresentAsync(administrationClient, topic.Name, timeout.Token);
        }
    }

    static async Task DeleteQueueIfPresentAsync(
        ServiceBusAdministrationClient administrationClient,
        string queue,
        CancellationToken cancellationToken)
    {
        try
        {
            await administrationClient.DeleteQueueAsync(queue, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
        }
    }

    static async Task DeleteTopicIfPresentAsync(
        ServiceBusAdministrationClient administrationClient,
        string topic,
        CancellationToken cancellationToken)
    {
        try
        {
            await administrationClient.DeleteTopicAsync(topic, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
        }
    }

    public static string EntityName(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        string normalized = new(purpose.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        if (normalized.Length == 0)
            throw new ArgumentException("The entity purpose must contain an Azure name character.", nameof(purpose));

        return $"vsb-{normalized}-{Guid.NewGuid():N}";
    }
}
