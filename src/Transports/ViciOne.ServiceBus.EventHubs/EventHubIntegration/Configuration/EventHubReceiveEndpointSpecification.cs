using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides an event hub receive endpoint specification implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="hostSettings">The host settings value.</param>
    /// <param name="storageSettings">The storage settings value.</param>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Gets the endpoint name value.
    /// </summary>
    public string EndpointName { get; }

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
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates receive endpoint.
    /// </summary>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
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
            throw new ConfigurationException(result, "An exception occurred creating EventHub receive endpoint", ex);
        }
    }
}
