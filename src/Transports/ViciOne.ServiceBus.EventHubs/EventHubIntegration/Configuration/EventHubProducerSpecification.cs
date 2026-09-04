using System;
using System.Collections.Generic;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides an event hub producer specification implementation.
/// </summary>
public class EventHubProducerSpecification :
    IEventHubProducerConfigurator,
    IEventHubProducerSpecification
{
    readonly List<Action<ISendPipeConfigurator>> _configureSend;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly IHostSettings _hostSettings;
    readonly SendObservable _sendObservers;
    readonly ISerializationConfiguration _serializationConfiguration;
    Action<EventHubProducerClientOptions>? _configureOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="hostSettings">The host settings value.</param>
    public EventHubProducerSpecification(IEventHubHostConfiguration hostConfiguration, IHostSettings hostSettings)
    {
        _hostConfiguration = hostConfiguration;
        _hostSettings = hostSettings;
        _serializationConfiguration = new SerializationConfiguration();
        _sendObservers = new SendObservable();
        _configureSend = new List<Action<ISendPipeConfigurator>>();
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
    }

    /// <summary>
    /// Configures send.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        _configureSend.Add(callback ?? throw new ArgumentNullException(nameof(callback)));
    }

    /// <summary>
    /// Gets or sets the configure options value.
    /// </summary>
    public Action<EventHubProducerClientOptions>? ConfigureOptions
    {
        set => _configureOptions = value ?? throw new ArgumentNullException(nameof(value));
        get => _configureOptions;
    }

    /// <summary>
    /// Adds serializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isSerializer">The is serializer value.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _serializationConfiguration.AddSerializer(factory, isSerializer);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(_hostSettings.ConnectionString)
            && (string.IsNullOrWhiteSpace(_hostSettings.FullyQualifiedNamespace) || _hostSettings.TokenCredential == null))
            yield return this.Failure("HostSettings", "is invalid");
    }

    /// <summary>
    /// Creates send transport context.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    public EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance)
    {
        var sendConfiguration = new SendPipeConfiguration(busInstance.HostConfiguration.Topology.SendTopology);
        for (var i = 0; i < _configureSend.Count; i++)
            _configureSend[i].Invoke(sendConfiguration.Configurator);

        var sendPipe = sendConfiguration.CreatePipe();

        var supervisor = new ProducerContextSupervisor(_hostConfiguration.ConnectionContextSupervisor, eventHubName);

        var transportContext = new EventHubProducerSendTransportContext(supervisor, sendPipe, busInstance.HostConfiguration, eventHubName,
            _serializationConfiguration.CreateSerializerCollection());
        transportContext.ConnectSendObserver(_sendObservers);

        return transportContext;
    }
}
