namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines an endpoint in a transport-independent way.</summary>
public interface IEndpointDefinition
{
    /// <summary>
    /// Gets whether the endpoint and its broker resources are removed when the endpoint stops.
    /// </summary>
    bool IsTemporary { get; }

    /// <summary>
    /// Gets the broker-specific number of messages fetched ahead of processing, when explicitly configured.
    /// </summary>
    int? PrefetchCount { get; }

    /// <summary>
    /// Gets the maximum number of messages delivered concurrently to the endpoint, when explicitly configured.
    /// </summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets whether the transport creates the consume topology required to route messages to the endpoint.</summary>
    bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the configured endpoint name or derives one with the supplied formatter.</summary>
    /// <param name="formatter">The naming convention used when no explicit name is configured.</param>
    /// <returns>The endpoint name.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);

    /// <summary>Applies the definition to a transport-specific receive endpoint.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context available to configuration callbacks.</param>
    void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context = null)
        where TEndpointConfigurator : IReceiveEndpointConfigurator;
}


/// <summary>Associates an endpoint definition with the component registered on that endpoint.</summary>
/// <typeparam name="TRegistration">The consumer, saga, future, or activity registration owned by the endpoint.</typeparam>
public interface IEndpointDefinition<TRegistration> :
    IEndpointDefinition
    where TRegistration : class
{
}
