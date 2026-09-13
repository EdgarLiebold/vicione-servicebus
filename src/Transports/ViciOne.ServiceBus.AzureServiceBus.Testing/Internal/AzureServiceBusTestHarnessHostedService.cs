using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Performs the configured Azure Service Bus test-namespace preparation during host startup.</summary>
internal sealed class AzureServiceBusTestHarnessHostedService :
    IHostedService
{
    readonly Func<ServiceBusAdministrationClient> _createAdministrationClient;
    readonly ILogger<AzureServiceBusTestHarnessHostedService> _logger;
    readonly AzureServiceBusTestHarnessOptions _testOptions;
    readonly AzureServiceBusTransportOptions _transportOptions;

    /// <summary>Initializes the hosted service from transport and test-harness options.</summary>
    /// <param name="transportOptions">The options containing the namespace connection string.</param>
    /// <param name="testOptions">The options that control startup cleanup.</param>
    /// <param name="logger">The logger used to report deleted entities.</param>
    public AzureServiceBusTestHarnessHostedService(IOptions<AzureServiceBusTransportOptions> transportOptions,
        IOptions<AzureServiceBusTestHarnessOptions> testOptions, ILogger<AzureServiceBusTestHarnessHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        ArgumentNullException.ThrowIfNull(testOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _transportOptions = transportOptions.Value;
        _testOptions = testOptions.Value;
        _createAdministrationClient = () => new ServiceBusAdministrationClient(_transportOptions.ConnectionString);
    }

    /// <summary>Initializes the hosted service with an explicit administration-client factory.</summary>
    /// <param name="transportOptions">The effective Azure Service Bus transport options.</param>
    /// <param name="testOptions">The effective test-harness options.</param>
    /// <param name="logger">The logger used to report deleted entities.</param>
    /// <param name="createAdministrationClient">Creates the administration client used for cleanup.</param>
    internal AzureServiceBusTestHarnessHostedService(
        AzureServiceBusTransportOptions transportOptions,
        AzureServiceBusTestHarnessOptions testOptions,
        ILogger<AzureServiceBusTestHarnessHostedService> logger,
        Func<ServiceBusAdministrationClient> createAdministrationClient)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        ArgumentNullException.ThrowIfNull(testOptions);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(createAdministrationClient);

        _logger = logger;
        _transportOptions = transportOptions;
        _testOptions = testOptions;
        _createAdministrationClient = createAdministrationClient;
    }

    /// <summary>Cleans the namespace when startup cleanup is enabled.</summary>
    /// <param name="cancellationToken">The token that cancels namespace enumeration and deletion.</param>
    /// <returns>A task that completes after optional namespace cleanup.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _testOptions.CleanNamespaceOnStart
            ? CleanNamespaceAsync(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Completes immediately because the hosted service owns no running background operation.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown before completion is reported.</param>
    /// <returns>A completed task, or a canceled task when shutdown has already been canceled.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    async Task CleanNamespaceAsync(CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient managementClient = _createAdministrationClient();
        if (managementClient == null)
        {
            throw new InvalidOperationException(
                "The Azure Service Bus administration-client factory returned null.");
        }

        AzureServiceBusCleanupResult result = await AzureServiceBusNamespaceCleaner
            .CleanAsync(managementClient, cancellationToken)
            .ConfigureAwait(false);

        if (result.TopicCount > 0 || result.QueueCount > 0)
        {
            _logger.LogInformation(
                "Removed {QueueCount} queue(s) and {TopicCount} topic(s)",
                result.QueueCount,
                result.TopicCount);
        }
    }
}
