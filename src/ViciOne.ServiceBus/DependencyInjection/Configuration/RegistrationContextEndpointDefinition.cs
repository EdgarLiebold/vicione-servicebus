namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a registration context endpoint definition implementation.
/// </summary>
public class RegistrationContextEndpointDefinition :
    IEndpointDefinition
{
    readonly IBusRegistrationContext _context;
    readonly IEndpointDefinition _endpointDefinition;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    /// <param name="context">The operation context.</param>
    public RegistrationContextEndpointDefinition(IEndpointDefinition endpointDefinition, IBusRegistrationContext context)
    {
        _endpointDefinition = endpointDefinition;
        _context = context;
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => _endpointDefinition.ConfigureConsumeTopology;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointDefinition.GetEndpointName(formatter);
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
        _endpointDefinition.Configure(configurator, context ?? _context);
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => _endpointDefinition.IsTemporary;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => _endpointDefinition.PrefetchCount;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => _endpointDefinition.ConcurrentMessageLimit;
}
