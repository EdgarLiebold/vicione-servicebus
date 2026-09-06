using System;
using System.Collections.Generic;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Collects serializer, observer, Azure SDK, and send-pipeline configuration for Event Hubs producers.</summary>
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

    /// <summary>Creates a producer specification for the configured Event Hubs host.</summary>
    /// <param name="hostConfiguration">The Event Hubs host configuration.</param>
    /// <param name="hostSettings">The namespace connection settings validated by this specification.</param>
    public EventHubProducerSpecification(IEventHubHostConfiguration hostConfiguration, IHostSettings hostSettings)
    {
        _hostConfiguration = hostConfiguration;
        _hostSettings = hostSettings;
        _serializationConfiguration = new SerializationConfiguration();
        _sendObservers = new SendObservable();
        _configureSend = new List<Action<ISendPipeConfigurator>>();
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
    }

    /// <summary>Adds a callback that configures the producer send pipeline.</summary>
    /// <param name="callback">The send-pipeline configuration callback.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        _configureSend.Add(callback ?? throw new ArgumentNullException(nameof(callback)));
    }

    /// <summary>Gets or sets the callback applied to each producer client's options.</summary>
    public Action<EventHubProducerClientOptions>? ConfigureOptions
    {
        set => _configureOptions = value ?? throw new ArgumentNullException(nameof(value));
        get => _configureOptions;
    }

    /// <summary>Adds an outbound message serializer.</summary>
    /// <param name="factory">The serializer factory to add.</param>
    /// <param name="isSerializer">Whether this factory becomes the default outbound serializer.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _serializationConfiguration.AddSerializer(factory, isSerializer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(_hostSettings.ConnectionString)
            && (string.IsNullOrWhiteSpace(_hostSettings.FullyQualifiedNamespace) || _hostSettings.TokenCredential == null))
            yield return this.Failure("HostSettings", "is invalid");
    }

    /// <summary>Builds the send pipeline and transport context for a named Event Hub.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="busInstance">The bus instance supplying host and topology services.</param>
    /// <returns>The configured Event Hubs send transport context.</returns>
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
