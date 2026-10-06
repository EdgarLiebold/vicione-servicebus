using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Collects and builds the host, storage, endpoint, producer, and pipeline configuration for an Event Hubs rider.</summary>
public class EventHubFactoryConfigurator :
    IEventHubFactoryConfigurator,
    IEventHubHostConfiguration
{
    readonly Recycle<IConnectionContextSupervisor> _connectionContextSupervisor;
    readonly ReceiveEndpointObservable _endpointObservers;
    readonly List<IEventHubReceiveEndpointSpecification> _endpoints;
    readonly HostSettings _hostSettings;
    readonly EventHubProducerSpecification _producerSpecification;
    readonly StorageSettings _storageSettings;
    bool _isHostSettingsConfigured;
    bool _isStorageSettingsConfigured;

    /// <summary>Creates an empty rider configuration with recyclable connection supervision.</summary>
    public EventHubFactoryConfigurator()
    {
        _endpointObservers = new ReceiveEndpointObservable();
        _endpoints = new List<IEventHubReceiveEndpointSpecification>();
        _hostSettings = new HostSettings();
        _storageSettings = new StorageSettings();

        _producerSpecification = new EventHubProducerSpecification(this, _hostSettings);

        _connectionContextSupervisor = new Recycle<IConnectionContextSupervisor>(() =>
            new ConnectionContextSupervisor(_hostSettings, _storageSettings, _producerSpecification.ConfigureOptions));
    }

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointObservers.Connect(observer);
    }

    /// <summary>Configures namespace access from an Event Hubs connection string.</summary>
    /// <param name="connectionString">The namespace connection string.</param>
    public void Host(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(nameof(connectionString));

        ThrowIfHostIsAlreadyConfigured();

        _hostSettings.ConnectionString = connectionString;
    }

    /// <summary>Configures namespace access with token-based Azure authentication.</summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace.</param>
    /// <param name="tokenCredential">The Azure credential used to authorize Event Hubs operations.</param>
    public void Host(string fullyQualifiedNamespace, TokenCredential tokenCredential)
    {
        if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            throw new ArgumentException(nameof(fullyQualifiedNamespace));
        if (tokenCredential == null)
            throw new ArgumentNullException(nameof(tokenCredential));

        ThrowIfHostIsAlreadyConfigured();

        _hostSettings.FullyQualifiedNamespace = fullyQualifiedNamespace;
        _hostSettings.TokenCredential = tokenCredential;
    }

    /// <summary>Configures the Blob Storage checkpoint store from a connection string.</summary>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <param name="configure">Optionally configures Blob client options.</param>
    public void Storage(string connectionString, Action<BlobClientOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(nameof(connectionString));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ConnectionString = connectionString;
        _storageSettings.Configure = configure;
    }

    /// <summary>Configures the Blob Storage checkpoint container from its URI.</summary>
    /// <param name="containerUri">The checkpoint container URI.</param>
    /// <param name="configure">Optionally configures Blob client options.</param>
    public void Storage(Uri containerUri, Action<BlobClientOptions>? configure = null)
    {
        if (containerUri == null)
            throw new ArgumentException(nameof(containerUri));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ContainerUri = containerUri;
        _storageSettings.Configure = configure;
    }

    /// <summary>Configures the Blob Storage checkpoint container with token-based Azure authentication.</summary>
    /// <param name="containerUri">The checkpoint container URI.</param>
    /// <param name="credential">The Azure credential used to authorize Blob client operations.</param>
    /// <param name="configure">Optionally configures Blob client options.</param>
    public void Storage(Uri containerUri, TokenCredential credential, Action<BlobClientOptions>? configure = null)
    {
        if (containerUri == null)
            throw new ArgumentException(nameof(containerUri));
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ContainerUri = containerUri;
        _storageSettings.TokenCredential = credential;
        _storageSettings.Configure = configure;
    }

    /// <summary>Configures the Blob Storage checkpoint container with a shared-key credential.</summary>
    /// <param name="containerUri">The checkpoint container URI.</param>
    /// <param name="credential">The storage account shared-key credential.</param>
    /// <param name="configure">Optionally configures Blob client options.</param>
    public void Storage(Uri containerUri, StorageSharedKeyCredential credential, Action<BlobClientOptions>? configure = null)
    {
        if (containerUri == null)
            throw new ArgumentException(nameof(containerUri));
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ContainerUri = containerUri;
        _storageSettings.SharedKeyCredential = credential;
        _storageSettings.Configure = configure;
    }

    /// <summary>Adds a receive-endpoint specification for an Event Hub consumer group.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the receive endpoint.</param>
    public void ReceiveEndpoint(string eventHubName, string consumerGroup, Action<IEventHubReceiveEndpointConfigurator> configure)
    {
        var specification = CreateSpecification(eventHubName, consumerGroup, configure);
        _endpoints.Add(specification);
    }

    /// <summary>Adds an outbound serializer to the producer specification.</summary>
    /// <param name="factory">The serializer factory to add.</param>
    /// <param name="isSerializer">Whether this factory becomes the default outbound serializer.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _producerSpecification.AddSerializer(factory, isSerializer);
    }

    /// <summary>Sets the single callback used to configure producer client options.</summary>
    /// <param name="configure">Applies changes to each producer client's options.</param>
    public void ConfigureProducerOptions(Action<EventHubProducerClientOptions> configure)
    {
        if (_producerSpecification.ConfigureOptions != null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Factory", "unknown", "ProducerOptions configurator may not be specified more than once.", "Correct the named configuration before starting the host"));
        _producerSpecification.ConfigureOptions = configure;
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _producerSpecification.ConnectSendObserver(observer);
    }

    /// <summary>Adds a callback that configures the producer send pipeline.</summary>
    /// <param name="callback">The send-pipeline configuration callback.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        _producerSpecification.ConfigureSend(callback);
    }

    /// <summary>Creates the send transport context for a named Event Hub.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="busInstance">The bus instance supplying host and topology services.</param>
    /// <returns>The configured Event Hubs send transport context.</returns>
    public EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance)
    {
        return _producerSpecification.CreateSendTransportContext(eventHubName, busInstance);
    }

    /// <summary>Creates a receive-endpoint specification without adding it to the rider's configured endpoint collection.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the receive endpoint when it is built.</param>
    /// <returns>The Event Hubs receive-endpoint specification.</returns>
    public IEventHubReceiveEndpointSpecification CreateSpecification(string eventHubName, string consumerGroup,
        Action<IEventHubReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(eventHubName))
            throw new ArgumentException(nameof(eventHubName));
        if (string.IsNullOrWhiteSpace(consumerGroup))
            throw new ArgumentException(nameof(consumerGroup));

        var specification = new EventHubReceiveEndpointSpecification(this, eventHubName, consumerGroup, _hostSettings, _storageSettings, configure);
        specification.ConnectReceiveEndpointObserver(_endpointObservers);
        return specification;
    }

    /// <summary>Gets the recyclable supervisor for shared Event Hubs connection state.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContextSupervisor.Supervisor;

    /// <summary>Builds an Event Hubs rider and all statically configured receive endpoints.</summary>
    /// <param name="context">The rider registration context.</param>
    /// <param name="busInstance">The bus instance that will own the rider.</param>
    /// <returns>The configured Event Hubs rider.</returns>
    public IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance)
    {
        var sendObserverHandle = ConnectSendObserver(busInstance.HostConfiguration.SendObservers);

        try
        {
            var endpoints = new ReceiveEndpointCollection();
            foreach (var endpoint in _endpoints)
                endpoints.Add(endpoint.EndpointName, endpoint.CreateReceiveEndpoint(busInstance));

            return new EventHubRider(this, busInstance, endpoints, context);
        }
        catch (Exception buildFailure)
        {
            try
            {
                sendObserverHandle.Disconnect();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Event Hubs rider construction and observer cleanup both failed.", buildFailure, cleanupFailure);
            }

            throw;
        }
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (KeyValuePair<string, IEventHubReceiveEndpointSpecification[]> kv in _endpoints.GroupBy(x => x.EndpointName)
                     .ToDictionary(x => x.Key, x => x.ToArray()))
        {
            if (kv.Value.Length > 1)
                yield return this.Failure($"EventHub: {kv.Key} was added more than once.");

            foreach (var result in kv.Value.SelectMany(x => x.Validate()))
                yield return result;
        }

        foreach (var result in _producerSpecification.Validate())
            yield return result;
    }

    /// <summary>Builds the bus-instance specification used to attach this rider during bus creation.</summary>
    /// <param name="context">The rider registration context.</param>
    /// <returns>The Event Hubs bus-instance specification.</returns>
    public IBusInstanceSpecification Build(IRiderRegistrationContext context)
    {
        return new EventHubBusInstanceSpecification(context, this);
    }

    void ThrowIfHostIsAlreadyConfigured()
    {
        if (_isHostSettingsConfigured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Factory", "unknown", "Host settings may not be specified more than once.", "Correct the named configuration before starting the host"));
        _isHostSettingsConfigured = true;
    }

    void ThrowIfStorageIsAlreadyConfigured()
    {
        if (_isStorageSettingsConfigured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Factory", "unknown", "Storage settings may not be specified more than once.", "Correct the named configuration before starting the host"));
        _isStorageSettingsConfigured = true;
    }
}
