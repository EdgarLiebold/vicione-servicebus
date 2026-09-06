using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Stores, validates, and builds one Event Hubs receive endpoint.</summary>
public class EventHubReceiveEndpointSpecification :
    IEventHubReceiveEndpointSpecification
{
    readonly Action<IEventHubReceiveEndpointConfigurator> _configure;
    readonly string _consumerGroup;
    readonly ReceiveEndpointObservable _endpointObservers;
    readonly string _eventHubName;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly IHostSettings _hostSettings;
    readonly IStorageSettings _storageSettings;

    /// <summary>Creates a receive-endpoint specification from rider and endpoint settings.</summary>
    /// <param name="hostConfiguration">The Event Hubs rider host configuration.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="hostSettings">The namespace connection settings.</param>
    /// <param name="storageSettings">The Blob Storage checkpoint settings.</param>
    /// <param name="configure">Configures the endpoint immediately before it is built.</param>
    public EventHubReceiveEndpointSpecification(IEventHubHostConfiguration hostConfiguration, string eventHubName, string consumerGroup,
        IHostSettings hostSettings, IStorageSettings storageSettings,
        Action<IEventHubReceiveEndpointConfigurator> configure)
    {
        _hostConfiguration = hostConfiguration;
        _eventHubName = eventHubName;
        _consumerGroup = consumerGroup;
        _hostSettings = hostSettings;
        _storageSettings = storageSettings;
        _configure = configure;
        EndpointName = $"{EventHubEndpointAddress.PathPrefix}/{_eventHubName}";
        if (!string.IsNullOrWhiteSpace(_consumerGroup))
            EndpointName = $"{EndpointName}/{_consumerGroup}";

        _endpointObservers = new ReceiveEndpointObservable();
    }

    /// <summary>Gets the bus endpoint name derived from the Event Hub and consumer group.</summary>
    public string EndpointName { get; }

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointObservers.Connect(observer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(_eventHubName))
            yield return this.Failure("EventHubName", "should not be empty");

        if (string.IsNullOrWhiteSpace(_consumerGroup))
            yield return this.Failure("ConsumerGroup", "should not be empty");

        if (string.IsNullOrWhiteSpace(_hostSettings.ConnectionString)
            && (string.IsNullOrWhiteSpace(_hostSettings.FullyQualifiedNamespace) || _hostSettings.TokenCredential == null))
            yield return this.Failure("HostSettings", "is invalid");

        if (string.IsNullOrWhiteSpace(_storageSettings.ConnectionString) && _storageSettings.ContainerUri == null)
            yield return this.Failure("StorageSettings", "is invalid");
    }

    /// <summary>Validates, builds, and returns the receive endpoint.</summary>
    /// <param name="busInstance">The bus instance that will own the endpoint.</param>
    /// <returns>The configured receive endpoint.</returns>
    public ReceiveEndpoint CreateReceiveEndpoint(IBusInstance busInstance)
    {
        var endpointConfiguration = busInstance.HostConfiguration.CreateReceiveEndpointConfiguration(EndpointName);

        var configurator = new EventHubReceiveEndpointConfigurator(_hostConfiguration, busInstance, endpointConfiguration, _hostSettings,
            _storageSettings, _eventHubName, _consumerGroup);
        configurator.ConnectReceiveEndpointObserver(_endpointObservers);
        _configure?.Invoke(configurator);

        IReadOnlyList<ValidationResult> result = Validate().Concat(configurator.Validate())
            .ThrowIfContainsFailure($"{TypeCache.GetShortName(GetType())} configuration is invalid:");

        try
        {
            return configurator.Build();
        }
        catch (Exception ex)
        {
            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "An exception occurred creating EventHub receive endpoint", "Correct the named configuration before starting the host"), ex);
        }
    }
}
