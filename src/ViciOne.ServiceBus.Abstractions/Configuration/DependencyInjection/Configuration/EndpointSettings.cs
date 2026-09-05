using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an endpoint settings implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class EndpointSettings<TConsumer> :
    IEndpointSettings<TConsumer>
    where TConsumer : class
{
    List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>? _callbacks;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EndpointSettings()
    {
        ConfigureConsumeTopology = true;
    }

    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the is temporary value.
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>
    /// Gets or sets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; set; }

    /// <summary>
    /// Gets or sets the instance id value.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Configures endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void ConfigureEndpoint<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        if (_callbacks == null)
            return;

        foreach (Action<IRegistrationContext?, IReceiveEndpointConfigurator> callback in _callbacks)
            callback(context, configurator);
    }

    /// <summary>
    /// Adds configure endpoint callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator>? callback)
    {
        if (callback == null)
            return;

        _callbacks ??= new List<Action<IRegistrationContext?, IReceiveEndpointConfigurator>>(1);

        _callbacks.Add((_, cfg) => callback(cfg));
    }

    /// <summary>
    /// Adds configure endpoint callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
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
