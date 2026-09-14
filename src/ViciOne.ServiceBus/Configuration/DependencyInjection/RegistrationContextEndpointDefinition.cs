namespace ViciOne.ServiceBus.Configuration;

/// <summary>Binds an endpoint definition to the bus registration context that created it.</summary>
internal sealed class RegistrationContextEndpointDefinition :
    IEndpointDefinition
{
    readonly IBusRegistrationContext _context;
    readonly IEndpointDefinition _endpointDefinition;

    /// <summary>Creates a definition that supplies its captured bus context when no explicit context is provided.</summary>
    /// <param name="endpointDefinition">The endpoint definition to delegate to.</param>
    /// <param name="context">The bus registration context that owns the endpoint.</param>
    public RegistrationContextEndpointDefinition(IEndpointDefinition endpointDefinition, IBusRegistrationContext context)
    {
        _endpointDefinition = endpointDefinition ?? throw new ArgumentNullException(nameof(endpointDefinition));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Gets whether the delegated endpoint creates consume topology.</summary>
    public bool ConfigureConsumeTopology => _endpointDefinition.ConfigureConsumeTopology;

    /// <summary>Formats the delegated endpoint name.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The delegated endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return _endpointDefinition.GetEndpointName(formatter);
    }

    /// <summary>Applies the delegated definition with an explicit or captured registration context.</summary>
    /// <typeparam name="T">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">An optional context that replaces the captured bus context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _endpointDefinition.Configure(configurator, context ?? _context);
    }

    /// <summary>Gets whether the delegated endpoint and its broker resources are temporary.</summary>
    public bool IsTemporary => _endpointDefinition.IsTemporary;

    /// <summary>Gets the delegated prefetch count, if one is declared.</summary>
    public int? PrefetchCount => _endpointDefinition.PrefetchCount;

    /// <summary>Gets the delegated concurrent-delivery limit, if one is declared.</summary>
    public int? ConcurrentMessageLimit => _endpointDefinition.ConcurrentMessageLimit;
}
