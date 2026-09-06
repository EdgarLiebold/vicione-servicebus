using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures endpoint registration.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class EndpointRegistrationConfigurator<T> :
    IEndpointRegistrationConfigurator
    where T : class
{
    readonly EndpointSettings<IEndpointDefinition<T>> _settings;

    /// <summary>Initializes a new instance.</summary>
    public EndpointRegistrationConfigurator()
    {
        _settings = new EndpointSettings<IEndpointDefinition<T>>();
    }

    /// <summary>Gets the settings.</summary>
    public IEndpointSettings<IEndpointDefinition<T>> Settings => _settings;

    /// <summary>Gets or sets the name.</summary>
    public string Name
    {
        set => _settings.Name = value;
    }

    /// <summary>Gets or sets the temporary.</summary>
    public bool Temporary
    {
        set => _settings.IsTemporary = value;
    }

    /// <summary>Gets or sets the prefetch count.</summary>
    public int? PrefetchCount
    {
        set => _settings.PrefetchCount = value;
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _settings.ConcurrentMessageLimit = value;
    }

    /// <summary>Gets or sets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology
    {
        set => _settings.ConfigureConsumeTopology = value;
    }

    /// <summary>Gets or sets the instance id.</summary>
    public string InstanceId
    {
        set => _settings.InstanceId = value;
    }

    /// <summary>Adds configure endpoint callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator>? callback)
    {
        _settings.AddConfigureEndpointCallback(callback);
    }

    /// <summary>Adds configure endpoint callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointCallback(Action<IRegistrationContext, IReceiveEndpointConfigurator>? callback)
    {
        _settings.AddConfigureEndpointCallback(callback);
    }
}
