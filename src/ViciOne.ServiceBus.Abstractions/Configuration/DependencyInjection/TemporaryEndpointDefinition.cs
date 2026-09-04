namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Specifies a temporary endpoint, with the prefix "response"
/// </summary>
public class TemporaryEndpointDefinition :
    IEndpointDefinition
{
    readonly string _tag;
    string? _name;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="prefetchCount">The prefetch count value.</param>
    /// <param name="configureConsumeTopology">The configure consume topology value.</param>
    public TemporaryEndpointDefinition(string? tag = default, int? concurrentMessageLimit = default, int? prefetchCount = default,
        bool configureConsumeTopology = true)
    {
        ConcurrentMessageLimit = concurrentMessageLimit;
        PrefetchCount = prefetchCount;
        ConfigureConsumeTopology = configureConsumeTopology;

        _tag = tag ?? "endpoint";
    }

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _name ??= formatter.TemporaryEndpoint(_tag);
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => true;
    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount { get; }
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; }
    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
