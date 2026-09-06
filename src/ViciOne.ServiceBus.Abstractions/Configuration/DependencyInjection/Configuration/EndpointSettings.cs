using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines settings for endpoint.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class EndpointSettings<TConsumer> :
    IEndpointSettings<TConsumer>
    where TConsumer : class
{
    List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>? _callbacks;

    /// <summary>Initializes a new instance.</summary>
    public EndpointSettings()
    {
        ConfigureConsumeTopology = true;
    }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets a value indicating whether temporary.</summary>
    public bool IsTemporary { get; set; }

    /// <summary>Gets or sets the prefetch count.</summary>
    public int? PrefetchCount { get; set; }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>Gets or sets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology { get; set; }

    /// <summary>Gets or sets the instance id.</summary>
    public string? InstanceId { get; set; }

    /// <summary>Configures endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void ConfigureEndpoint<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        if (_callbacks == null)
            return;

        foreach (Action<IRegistrationContext?, IReceiveEndpointConfigurator> callback in _callbacks)
            callback(context, configurator);
    }

    /// <summary>Adds configure endpoint callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator>? callback)
    {
        if (callback == null)
            return;

        _callbacks ??= new List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>(1);

        _callbacks.Add((_, cfg) => callback(cfg));
    }

    /// <summary>Adds configure endpoint callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointCallback(Action<IRegistrationContext, IReceiveEndpointConfigurator>? callback)
    {
        if (callback == null)
            return;

        _callbacks ??= new List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>(1);

        _callbacks.Add((context, cfg) =>
        {
            if (context is null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Endpoint Settings", "unknown", "The bus registration context cannot be null (via AddConfigureEndpointCallback).", "Correct the named configuration before starting the host"));

            callback(context, cfg);
        });
    }
}
