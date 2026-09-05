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

/// <summary>
/// Provides an event hub factory configurator implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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

    /// <summary>
    /// Connects receive endpoint observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointObservers.Connect(observer);
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    public void Host(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(nameof(connectionString));

        ThrowIfHostIsAlreadyConfigured();

        _hostSettings.ConnectionString = connectionString;
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified namespace value.</param>
    /// <param name="tokenCredential">The token credential value.</param>
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

    /// <summary>
    /// Performs the storage operation.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Storage(string connectionString, Action<BlobClientOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(nameof(connectionString));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ConnectionString = connectionString;
        _storageSettings.Configure = configure;
    }

    /// <summary>
    /// Performs the storage operation.
    /// </summary>
    /// <param name="containerUri">The container uri value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Storage(Uri containerUri, Action<BlobClientOptions>? configure = null)
    {
        if (containerUri == null)
            throw new ArgumentException(nameof(containerUri));

        ThrowIfStorageIsAlreadyConfigured();

        _storageSettings.ContainerUri = containerUri;
        _storageSettings.Configure = configure;
    }

    /// <summary>
    /// Performs the storage operation.
    /// </summary>
    /// <param name="containerUri">The container uri value.</param>
    /// <param name="credential">The credential value.</param>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Performs the storage operation.
    /// </summary>
    /// <param name="containerUri">The container uri value.</param>
    /// <param name="credential">The credential value.</param>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void ReceiveEndpoint(string eventHubName, string consumerGroup, Action<IEventHubReceiveEndpointConfigurator> configure)
    {
        var specification = CreateSpecification(eventHubName, consumerGroup, configure);
        _endpoints.Add(specification);
    }

    /// <summary>
    /// Adds serializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isSerializer">The is serializer value.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _producerSpecification.AddSerializer(factory, isSerializer);
    }

    /// <summary>
    /// Configures producer options.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureProducerOptions(Action<EventHubProducerClientOptions> configure)
    {
        if (_producerSpecification.ConfigureOptions != null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Factory", "unknown", "ProducerOptions configurator may not be specified more than once.", "Correct the named configuration before starting the host"));
        _producerSpecification.ConfigureOptions = configure;
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _producerSpecification.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Configures send.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        _producerSpecification.ConfigureSend(callback);
    }

    /// <summary>
    /// Creates send transport context.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    public EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance)
    {
        return _producerSpecification.CreateSendTransportContext(eventHubName, busInstance);
    }

    /// <summary>
    /// Creates specification.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContextSupervisor.Supervisor;

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    public IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance)
    {
        ConnectSendObserver(busInstance.HostConfiguration.SendObservers);

        var endpoints = new ReceiveEndpointCollection();
        foreach (var endpoint in _endpoints)
            endpoints.Add(endpoint.EndpointName, endpoint.CreateReceiveEndpoint(busInstance));

        return new EventHubRider(this, busInstance, endpoints, context);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
