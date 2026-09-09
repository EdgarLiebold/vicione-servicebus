using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transport-independent settings for a registered receive endpoint.</summary>
/// <typeparam name="TRegistration">The consumer, saga, future, or activity registration owned by the endpoint.</typeparam>
public class EndpointRegistrationConfigurator<TRegistration> :
    IEndpointRegistrationConfigurator
    where TRegistration : class
{
    readonly EndpointSettings<IEndpointDefinition<TRegistration>> _settings;

    /// <summary>Creates an endpoint registration with consume-topology configuration enabled.</summary>
    public EndpointRegistrationConfigurator()
    {
        _settings = new EndpointSettings<IEndpointDefinition<TRegistration>>();
    }

    /// <summary>Gets the configured endpoint settings.</summary>
    public IEndpointSettings<IEndpointDefinition<TRegistration>> Settings => _settings;

    /// <summary>Sets an explicit endpoint name.</summary>
    public string Name
    {
        set => _settings.Name = value;
    }

    /// <summary>Sets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    public bool Temporary
    {
        set => _settings.IsTemporary = value;
    }

    /// <summary>Sets the broker-specific number of messages fetched ahead of processing.</summary>
    public int? PrefetchCount
    {
        set => _settings.PrefetchCount = value;
    }

    /// <summary>Sets the maximum number of messages processed concurrently on the endpoint.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _settings.ConcurrentMessageLimit = value;
    }

    /// <summary>Sets whether the transport creates the endpoint's consume topology.</summary>
    public bool ConfigureConsumeTopology
    {
        set => _settings.ConfigureConsumeTopology = value;
    }

    /// <summary>Sets the identifier appended to the endpoint name.</summary>
    public string InstanceId
    {
        set => _settings.InstanceId = value;
    }

    /// <summary>Adds a callback that configures the transport-specific receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    public void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator> callback)
    {
        _settings.AddConfigureEndpointCallback(callback);
    }

    /// <summary>Adds a callback that can resolve registered services while configuring the receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    public void AddConfigureEndpointCallback(Action<IRegistrationContext, IReceiveEndpointConfigurator> callback)
    {
        _settings.AddConfigureEndpointCallback(callback);
    }
}
