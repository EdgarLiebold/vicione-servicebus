using System;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a host settings implementation.
/// </summary>
public class HostSettings :
    ServiceBusHostSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public HostSettings()
    {
        RetryMinBackoff = TimeSpan.FromMilliseconds(100);
        RetryMaxBackoff = TimeSpan.FromSeconds(30);
        RetryLimit = 3;

        TransportType = ServiceBusTransportType.AmqpTcp;

        ServiceUri = new Uri("sb://no-host-configured");
    }

    /// <summary>
    /// Gets or sets the service uri value.
    /// </summary>
    public Uri ServiceUri { get; set; }
    /// <summary>
    /// Gets or sets the service bus client value.
    /// </summary>
    public ServiceBusClient? ServiceBusClient { get; set; }
    /// <summary>
    /// Gets or sets the service bus administration client value.
    /// </summary>
    public ServiceBusAdministrationClient? ServiceBusAdministrationClient { get; set; }
    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public string? ConnectionString { get; set; }
    /// <summary>
    /// Gets or sets the named key credential value.
    /// </summary>
    public AzureNamedKeyCredential? NamedKeyCredential { get; set; }
    /// <summary>
    /// Gets or sets the sas credential value.
    /// </summary>
    public AzureSasCredential? SasCredential { get; set; }
    /// <summary>
    /// Gets or sets the token credential value.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
    /// <summary>
    /// Gets or sets the retry min backoff value.
    /// </summary>
    public TimeSpan RetryMinBackoff { get; set; }
    /// <summary>
    /// Gets or sets the retry max backoff value.
    /// </summary>
    public TimeSpan RetryMaxBackoff { get; set; }
    /// <summary>
    /// Gets or sets the retry limit value.
    /// </summary>
    public int RetryLimit { get; set; }
    /// <summary>
    /// Gets or sets the transport type value.
    /// </summary>
    public ServiceBusTransportType TransportType { get; set; }
}
