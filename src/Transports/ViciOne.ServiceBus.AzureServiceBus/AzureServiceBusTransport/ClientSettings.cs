using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes the Azure Service Bus entity and processor settings used to create a client context.</summary>
public interface ClientSettings
{
    /// <summary>Gets the maximum number of concurrent message callbacks.</summary>
    int MaxConcurrentCalls { get; }

    /// <summary>Gets the number of messages the processor may cache locally.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the maximum duration for automatic message- or session-lock renewal.</summary>
    TimeSpan MaxAutoRenewDuration { get; }

    /// <summary>
    /// Gets the maximum idle time to wait for a message from an accepted session. When unset, the SDK uses
    /// <see
    /// href="https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusretryoptions.trytimeout?view=azure-dotnet#azure-messaging-servicebus-servicebusretryoptions-trytimeout">
    /// ServiceBusRetryOptions.TryTimeout
    /// </see>.
    /// </summary>
    TimeSpan? SessionIdleTimeout { get; }

    /// <summary>Gets the maximum number of sessions processed concurrently.</summary>
    int MaxConcurrentSessions { get; }

    /// <summary>Gets the maximum number of concurrent message callbacks for each session.</summary>
    int MaxConcurrentCallsPerSession { get; }

    /// <summary>Gets the queue or subscription entity path.</summary>
    string Path { get; }

    /// <summary>Gets the logical entity name.</summary>
    string Name { get; }

    /// <summary>Builds the transport input address for the entity path in a namespace.</summary>
    /// <param name="serviceUri">The namespace URI.</param>
    /// <param name="path">The entity path relative to the namespace.</param>
    /// <returns>The resolved entity address.</returns>
    Uri GetInputAddress(Uri serviceUri, string path);
}
