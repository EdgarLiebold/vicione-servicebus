namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes transport-independent settings and callbacks for an endpoint definition.</summary>
/// <typeparam name="TDefinition">The endpoint definition associated with the settings.</typeparam>
public interface IEndpointSettings<TDefinition>
    where TDefinition : class
{
    /// <summary>Gets the explicit endpoint name, when configured.</summary>
    string? Name { get; }

    /// <summary>Gets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    bool IsTemporary { get; }

    /// <summary>Gets the broker-specific number of messages fetched ahead of processing.</summary>
    int? PrefetchCount { get; }

    /// <summary>Gets the maximum number of messages processed concurrently on the endpoint.</summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets whether the transport creates the endpoint's consume topology.</summary>
    bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the identifier appended to the endpoint name, when configured.</summary>
    string? InstanceId { get; }

    /// <summary>Invokes the registered callbacks for a transport-specific receive endpoint.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context available to callbacks.</param>
    void ConfigureEndpoint<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator;
}
