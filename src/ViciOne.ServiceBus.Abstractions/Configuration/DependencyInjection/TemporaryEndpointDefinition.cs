namespace ViciOne.ServiceBus.Configuration;

/// <summary>Specifies a temporary endpoint, with the prefix "response".</summary>
public class TemporaryEndpointDefinition :
    IEndpointDefinition
{
    readonly string _tag;
    string? _name;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="tag">The tag.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit.</param>
    /// <param name="prefetchCount">The prefetch count.</param>
    /// <param name="configureConsumeTopology">The configure consume topology.</param>
    public TemporaryEndpointDefinition(string? tag = default, int? concurrentMessageLimit = default, int? prefetchCount = default,
        bool configureConsumeTopology = true)
    {
        ConcurrentMessageLimit = concurrentMessageLimit;
        PrefetchCount = prefetchCount;
        ConfigureConsumeTopology = configureConsumeTopology;

        _tag = tag ?? "endpoint";
    }

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _name ??= formatter.TemporaryEndpoint(_tag);
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => true;
    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount { get; }
    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; }
    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
