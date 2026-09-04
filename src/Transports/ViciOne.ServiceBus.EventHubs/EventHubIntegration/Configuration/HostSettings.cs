using Azure.Core;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides a host settings implementation.
/// </summary>
public class HostSettings :
    IHostSettings
{
    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public string? ConnectionString { get; set; }
    /// <summary>
    /// Gets or sets the fully qualified namespace value.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }
    /// <summary>
    /// Gets or sets the token credential value.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
}
