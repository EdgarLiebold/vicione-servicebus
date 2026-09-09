using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines an auto-deleting receive endpoint with a generated temporary name.</summary>
public class TemporaryEndpointDefinition :
    IEndpointDefinition
{
    readonly string _tag;
    string? _name;

    /// <summary>Creates a temporary endpoint definition.</summary>
    /// <param name="tag">The purpose tag included in the generated endpoint name.</param>
    /// <param name="concurrentMessageLimit">The optional maximum number of messages processed concurrently.</param>
    /// <param name="prefetchCount">The optional broker-specific number of messages fetched ahead of processing.</param>
    /// <param name="configureConsumeTopology">Whether the transport creates the endpoint's consume topology.</param>
    public TemporaryEndpointDefinition(string? tag = default, int? concurrentMessageLimit = default, int? prefetchCount = default,
        bool configureConsumeTopology = true)
    {
        if (tag is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        if (concurrentMessageLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(concurrentMessageLimit),
                concurrentMessageLimit,
                "The concurrent message limit must be positive.");
        }
        if (prefetchCount < 0)
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), prefetchCount, "The prefetch count cannot be negative.");

        ConcurrentMessageLimit = concurrentMessageLimit;
        PrefetchCount = prefetchCount;
        ConfigureConsumeTopology = configureConsumeTopology;

        _tag = tag ?? "endpoint";
    }

    /// <summary>Gets the generated temporary endpoint name.</summary>
    /// <param name="formatter">The naming convention used to generate the name.</param>
    /// <returns>The temporary endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return _name ??= formatter.TemporaryEndpoint(_tag);
    }

    /// <summary>Gets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    public bool IsTemporary => true;
    /// <summary>Gets the broker-specific number of messages fetched ahead of processing.</summary>
    public int? PrefetchCount { get; }
    /// <summary>Gets the maximum number of messages processed concurrently on the endpoint.</summary>
    public int? ConcurrentMessageLimit { get; }
    /// <summary>Gets whether the transport creates the endpoint's consume topology.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Validates the receive endpoint supplied for this definition.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint associated with the definition.</param>
    /// <param name="context">The optional registration context; temporary definitions have no callbacks to invoke.</param>
    public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
    }
}
