using System;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes an Azure Service Bus namespace connection and its retry policy.</summary>
public interface ServiceBusHostSettings
{
    /// <summary>Gets the namespace address and optional entity-path scope.</summary>
    Uri ServiceUri { get; }

    /// <summary>Gets the caller-supplied client used instead of constructing one from these settings.</summary>
    ServiceBusClient? ServiceBusClient { get; }

    /// <summary>Gets the caller-supplied administration client used instead of constructing one from these settings.</summary>
    ServiceBusAdministrationClient? ServiceBusAdministrationClient { get; }

    /// <summary>Gets the Azure shared access key credential.</summary>
    /// <remarks>
    /// This property cannot be used if <see cref="SasCredential" /> or <see cref="TokenCredential" />
    /// is being used.
    /// </remarks>
    AzureNamedKeyCredential? NamedKeyCredential { get; }

    /// <summary>Gets the precomputed shared access signature.</summary>
    /// <remarks>
    /// This property cannot be used if <see cref="NamedKeyCredential" /> or <see cref="TokenCredential" />
    /// is being used.
    /// </remarks>
    AzureSasCredential? SasCredential { get; }

    /// <summary>Gets the Azure token credential.</summary>
    /// <remarks>
    /// This property cannot be used if <see cref="SasCredential" /> or <see cref="NamedKeyCredential" />
    /// is being used.
    /// </remarks>
    TokenCredential? TokenCredential { get; }

    /// <summary>Gets the Azure Service Bus connection string.</summary>
    /// <remarks>
    /// If the connection string contains no credential, configure exactly one of
    /// <see cref="NamedKeyCredential" />, <see cref="SasCredential" />, or <see cref="TokenCredential" />.
    /// </remarks>
    string? ConnectionString { get; }

    /// <summary>Gets the minimum delay used by the exponential retry policy.</summary>
    TimeSpan RetryMinBackoff { get; }

    /// <summary>Gets the maximum delay used by the exponential retry policy.</summary>
    TimeSpan RetryMaxBackoff { get; }

    /// <summary>Gets the maximum number of retries for an Azure SDK operation.</summary>
    int RetryLimit { get; }

    /// <summary>Gets whether the Azure SDK uses AMQP over TCP or WebSockets.</summary>
    ServiceBusTransportType TransportType { get; }
}
