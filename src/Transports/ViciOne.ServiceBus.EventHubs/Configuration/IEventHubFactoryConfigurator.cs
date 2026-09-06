using System;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Configures the Event Hubs rider, its checkpoint store, receive endpoints, and producers.</summary>
public interface IEventHubFactoryConfigurator :
    IRiderFactoryConfigurator,
    ISendObserverConnector,
    ISendPipelineConfigurator
{
    /// <summary>Configures the Event Hubs namespace from a connection string.</summary>
    /// <param name="connectionString">
    /// The connection string used to connect to the Event Hubs namespace. It must include the authorization properties required by the Azure SDK.
    /// </param>
    void Host(string connectionString);

    /// <summary>Configures the Event Hubs namespace with token-based Azure authentication.</summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace, such as <c>my-namespace.servicebus.windows.net</c>.</param>
    /// <param name="tokenCredential">
    /// The Azure credential used to authorize Event Hubs client operations.
    /// </param>
    void Host(string fullyQualifiedNamespace, TokenCredential tokenCredential);

    /// <summary>Configures the Blob Storage checkpoint store from a connection string.</summary>
    /// <param name="connectionString">
    /// The Azure Storage connection string used by the checkpoint client.
    /// </param>
    /// <param name="configure">Optionally configures the Blob client.</param>
    void Storage(string connectionString, Action<BlobClientOptions>? configure = null);

    /// <summary>Configures the Blob Storage checkpoint container from its URI.</summary>
    /// <param name="containerUri">
    /// A <see cref="Uri" /> referencing the blob container that includes the
    /// name of the account and the name of the container.
    /// </param>
    /// <param name="configure">Optionally configures the Blob client.</param>
    void Storage(Uri containerUri, Action<BlobClientOptions>? configure = null);

    /// <summary>Configures the Blob Storage checkpoint container with token-based Azure authentication.</summary>
    /// <param name="containerUri">
    /// A <see cref="Uri" /> referencing the blob container that includes the
    /// name of the account and the name of the container.
    /// </param>
    /// <param name="credential">The Azure credential used to authorize Blob client operations.</param>
    /// <param name="configure">Optionally configures the Blob client.</param>
    void Storage(Uri containerUri, TokenCredential credential, Action<BlobClientOptions>? configure = null);

    /// <summary>Configures the Blob Storage checkpoint container with a shared-key credential.</summary>
    /// <param name="containerUri">
    /// A <see cref="Uri" /> referencing the blob container that includes the
    /// name of the account and the name of the container.
    /// </param>
    /// <param name="credential">The shared key credential used to sign requests.</param>
    /// <param name="configure">Optionally configures the Blob client.</param>
    void Storage(Uri containerUri, StorageSharedKeyCredential credential, Action<BlobClientOptions>? configure = null);

    /// <summary>Adds a receive endpoint for an Event Hub consumer group.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the receive endpoint.</param>
    void ReceiveEndpoint(string eventHubName, string consumerGroup, Action<IEventHubReceiveEndpointConfigurator> configure);

    /// <summary>Sets the outbound message serializer.</summary>
    /// <param name="factory">The serializer factory to add.</param>
    /// <param name="isSerializer">Whether this factory becomes the default outbound serializer.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>Configures Azure SDK options for every producer client created by this rider.</summary>
    /// <param name="configure">Applies changes to the producer client options.</param>
    void ConfigureProducerOptions(Action<EventHubProducerClientOptions> configure);
}
