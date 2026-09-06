namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for delegate endpoint.</summary>
public class DelegateEndpointDefinition :
    IEndpointDefinition
{
    readonly IDefinition _definition;
    readonly IEndpointDefinition? _endpointDefinition;
    readonly string _endpointName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public DelegateEndpointDefinition(string endpointName, IDefinition definition, IEndpointDefinition? endpointDefinition)
    {
        _endpointName = endpointName;
        _definition = definition;
        _endpointDefinition = endpointDefinition;
    }

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => _endpointDefinition?.ConfigureConsumeTopology ?? true;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointName;
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _endpointDefinition?.Configure(configurator, context);
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => _endpointDefinition?.IsTemporary ?? false;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => _endpointDefinition?.PrefetchCount;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _endpointDefinition?.ConcurrentMessageLimit;

    internal IEndpointDefinition? EndpointDefinition => _endpointDefinition;

    internal System.Type OwnerType => _definition.GetType();
}
