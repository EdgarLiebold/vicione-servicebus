using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores transport-independent settings and callbacks for an endpoint definition.</summary>
/// <typeparam name="TDefinition">The endpoint definition associated with the settings.</typeparam>
public class EndpointSettings<TDefinition> :
    IEndpointSettings<TDefinition>
    where TDefinition : class
{
    List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>? _callbacks;
    int? _concurrentMessageLimit;
    string? _instanceId;
    string? _name;
    int? _prefetchCount;

    /// <summary>Creates settings that configure consume topology by default.</summary>
    public EndpointSettings()
    {
        ConfigureConsumeTopology = true;
    }

    /// <summary>Gets or sets the explicit endpoint name.</summary>
    public string? Name
    {
        get => _name;
        set
        {
            if (value is not null)
                ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _name = value;
        }
    }

    /// <summary>Gets or sets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    public bool IsTemporary { get; set; }

    /// <summary>Gets or sets the broker-specific number of messages fetched ahead of processing.</summary>
    public int? PrefetchCount
    {
        get => _prefetchCount;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The prefetch count cannot be negative.");

            _prefetchCount = value;
        }
    }

    /// <summary>Gets or sets the maximum number of messages processed concurrently on the endpoint.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The concurrent message limit must be positive.");

            _concurrentMessageLimit = value;
        }
    }

    /// <summary>Gets or sets whether the transport creates the endpoint's consume topology.</summary>
    public bool ConfigureConsumeTopology { get; set; }

    /// <summary>Gets or sets the identifier appended to the endpoint name.</summary>
    public string? InstanceId
    {
        get => _instanceId;
        set
        {
            if (value is not null)
                ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _instanceId = value;
        }
    }

    /// <summary>Invokes the registered callbacks for a transport-specific receive endpoint.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context available to callbacks.</param>
    public void ConfigureEndpoint<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (_callbacks == null)
            return;

        foreach (Action<IRegistrationContext?, IReceiveEndpointConfigurator> callback in _callbacks)
            callback(context, configurator);
    }

    /// <summary>Adds a callback that configures the transport-specific receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    public void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _callbacks ??= new List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>(1);

        _callbacks.Add((_, cfg) => callback(cfg));
    }

    /// <summary>Adds a callback that can resolve registered services while configuring the receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    public void AddConfigureEndpointCallback(Action<IRegistrationContext, IReceiveEndpointConfigurator> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _callbacks ??= new List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>(1);

        _callbacks.Add((context, cfg) =>
        {
            if (context is null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Endpoint Settings",
                    "unknown",
                    "A registration-aware endpoint callback requires a bus registration context.",
                    "Supply the registration context when applying the endpoint definition"));

            callback(context, cfg);
        });
    }
}
