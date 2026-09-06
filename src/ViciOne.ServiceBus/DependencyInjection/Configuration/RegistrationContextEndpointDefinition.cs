namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for registration context endpoint.</summary>
public class RegistrationContextEndpointDefinition :
    IEndpointDefinition
{
    readonly IBusRegistrationContext _context;
    readonly IEndpointDefinition _endpointDefinition;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    /// <param name="context">The context associated with the operation.</param>
    public RegistrationContextEndpointDefinition(IEndpointDefinition endpointDefinition, IBusRegistrationContext context)
    {
        _endpointDefinition = endpointDefinition;
        _context = context;
    }

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => _endpointDefinition.ConfigureConsumeTopology;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointDefinition.GetEndpointName(formatter);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _endpointDefinition.Configure(configurator, context ?? _context);
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => _endpointDefinition.IsTemporary;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => _endpointDefinition.PrefetchCount;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _endpointDefinition.ConcurrentMessageLimit;
}
