using Azure.Core;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Provides the configured Event Hubs namespace authentication settings.</summary>
public interface IHostSettings
{
    /// <summary>
    /// Gets the namespace connection string, including the shared-access authorization properties but not an Event Hub entity path.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>Gets the fully qualified Event Hubs namespace used for token-based authentication.</summary>
    string? FullyQualifiedNamespace { get; }

    /// <summary>
    /// Gets the Azure credential used to authorize Event Hubs client operations.
    /// </summary>
    TokenCredential? TokenCredential { get; }
}
