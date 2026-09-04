namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delegate endpoint definition implementation.
/// </summary>
public class DelegateEndpointDefinition :
    IEndpointDefinition
{
    readonly IDefinition _definition;
    readonly IEndpointDefinition? _endpointDefinition;
    readonly string _endpointName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointName">The endpoint name value.</param>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public DelegateEndpointDefinition(string endpointName, IDefinition definition, IEndpointDefinition? endpointDefinition)
    {
        _endpointName = endpointName;
        _definition = definition;
        _endpointDefinition = endpointDefinition;
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => _endpointDefinition?.ConfigureConsumeTopology ?? true;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointName;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _endpointDefinition?.Configure(configurator, context);
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => _endpointDefinition?.IsTemporary ?? false;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => _endpointDefinition?.PrefetchCount;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => _endpointDefinition?.ConcurrentMessageLimit;

    internal IEndpointDefinition? EndpointDefinition => _endpointDefinition;

    internal System.Type OwnerType => _definition.GetType();
}
