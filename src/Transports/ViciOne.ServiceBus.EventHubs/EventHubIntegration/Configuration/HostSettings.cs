using Azure.Core;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Stores one of the supported Event Hubs namespace authentication configurations.</summary>
public class HostSettings :
    IHostSettings
{
    /// <summary>Gets or sets the namespace connection string.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets the fully qualified Event Hubs namespace used with <see cref="TokenCredential" />.</summary>
    public string? FullyQualifiedNamespace { get; set; }
    /// <summary>Gets or sets the Azure credential used with <see cref="FullyQualifiedNamespace" />.</summary>
    public TokenCredential? TokenCredential { get; set; }
}
