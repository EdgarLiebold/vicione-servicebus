using System;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Stores the resolved Azure Service Bus namespace, credential, retry, and transport settings.</summary>
public class HostSettings :
    ServiceBusHostSettings
{
    /// <summary>Initializes settings with the transport's retry defaults and AMQP-over-TCP.</summary>
    public HostSettings()
    {
        RetryMinBackoff = TimeSpan.FromMilliseconds(100);
        RetryMaxBackoff = TimeSpan.FromSeconds(30);
        RetryLimit = 3;

        TransportType = ServiceBusTransportType.AmqpTcp;

        ServiceUri = new Uri("sb://no-host-configured");
    }

    /// <summary>Gets or sets the Azure Service Bus namespace URI.</summary>
    public Uri ServiceUri { get; set; }
    /// <summary>Gets or sets a caller-supplied messaging client.</summary>
    public ServiceBusClient? ServiceBusClient { get; set; }
    /// <summary>Gets or sets a caller-supplied administration client.</summary>
    public ServiceBusAdministrationClient? ServiceBusAdministrationClient { get; set; }
    /// <summary>Gets or sets the namespace connection string used when no client instance is supplied.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets the shared-access key credential.</summary>
    public AzureNamedKeyCredential? NamedKeyCredential { get; set; }
    /// <summary>Gets or sets the shared-access signature credential.</summary>
    public AzureSasCredential? SasCredential { get; set; }
    /// <summary>Gets or sets the Azure identity token credential.</summary>
    public TokenCredential? TokenCredential { get; set; }
    /// <summary>Gets or sets the minimum delay between client retries.</summary>
    public TimeSpan RetryMinBackoff { get; set; }
    /// <summary>Gets or sets the maximum delay between client retries.</summary>
    public TimeSpan RetryMaxBackoff { get; set; }
    /// <summary>Gets or sets the maximum number of client retry attempts.</summary>
    public int RetryLimit { get; set; }
    /// <summary>Gets or sets the AMQP transport used by the Azure Service Bus client.</summary>
    public ServiceBusTransportType TransportType { get; set; }
}
