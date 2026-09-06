using System;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Creates checkpoint-container clients from the configured storage authentication mode.</summary>
internal static class EventHubCheckpointContainerClientFactory
{
    /// <summary>Creates a client for either a connection-string container name or a complete container URI.</summary>
    /// <param name="settings">The checkpoint storage settings.</param>
    /// <param name="endpointContainerName">The container name used only with connection-string storage configuration.</param>
    /// <returns>A client addressing the configured checkpoint container.</returns>
    public static BlobContainerClient Create(IStorageSettings settings, string endpointContainerName)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(endpointContainerName))
            throw new ArgumentException("A checkpoint container name is required.", nameof(endpointContainerName));

        var options = new BlobClientOptions();
        settings.Configure?.Invoke(options);

        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
            return new BlobContainerClient(settings.ConnectionString, endpointContainerName, options);

        Uri containerUri = settings.ContainerUri
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Receive endpoint",
                "unknown",
                "The Event Hub checkpoint storage container URI is not configured.",
                "Correct the named configuration before starting the host"));

        if (settings.TokenCredential is not null)
            return new BlobContainerClient(containerUri, settings.TokenCredential, options);

        return settings.SharedKeyCredential is not null
            ? new BlobContainerClient(containerUri, settings.SharedKeyCredential, options)
            : new BlobContainerClient(containerUri, options);
    }
}
