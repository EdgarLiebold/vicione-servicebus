namespace ViciOne.ServiceBus.Configuration;

/// <summary>Associates a registered component definition with its endpoint name and optional endpoint settings.</summary>
internal sealed class DelegateEndpointDefinition :
    IEndpointDefinition
{
    readonly IDefinition _definition;
    readonly IEndpointDefinition? _endpointDefinition;
    readonly string _endpointName;

    /// <summary>Creates a named endpoint view over a component definition and its optional endpoint settings.</summary>
    /// <param name="endpointName">The shared receive-endpoint name.</param>
    /// <param name="definition">The component definition that owns the endpoint declaration.</param>
    /// <param name="endpointDefinition">Optional endpoint settings declared by the component.</param>
    public DelegateEndpointDefinition(string endpointName, IDefinition definition, IEndpointDefinition? endpointDefinition)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException("Endpoint name must not be empty.", nameof(endpointName));

        _endpointName = endpointName;
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _endpointDefinition = endpointDefinition;
    }

    /// <summary>Gets whether consume topology is created for the delegated endpoint.</summary>
    public bool ConfigureConsumeTopology => _endpointDefinition?.ConfigureConsumeTopology ?? true;

    /// <summary>Returns the explicit shared endpoint name.</summary>
    /// <param name="formatter">The required naming convention; the explicit name is returned unchanged.</param>
    /// <returns>The explicit endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return _endpointName;
    }

    /// <summary>Applies the component's optional endpoint definition.</summary>
    /// <typeparam name="T">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The optional registration context supplied to the delegated definition.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _endpointDefinition?.Configure(configurator, context);
    }

    /// <summary>Gets whether the delegated endpoint and its broker resources are temporary.</summary>
    public bool IsTemporary => _endpointDefinition?.IsTemporary ?? false;

    /// <summary>Gets the delegated prefetch count, if one is declared.</summary>
    public int? PrefetchCount => _endpointDefinition?.PrefetchCount;

    /// <summary>Gets the delegated concurrent-delivery limit, if one is declared.</summary>
    public int? ConcurrentMessageLimit => _endpointDefinition?.ConcurrentMessageLimit;

    internal IEndpointDefinition? EndpointDefinition => _endpointDefinition;

    internal System.Type OwnerType => _definition.GetType();
}
