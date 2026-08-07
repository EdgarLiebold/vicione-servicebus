// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration;

public class RegistrationContextEndpointDefinition :
    IEndpointDefinition
{
    readonly IBusRegistrationContext _context;
    readonly IEndpointDefinition _endpointDefinition;

    public RegistrationContextEndpointDefinition(IEndpointDefinition endpointDefinition, IBusRegistrationContext context)
    {
        _endpointDefinition = endpointDefinition;
        _context = context;
    }

    public bool ConfigureConsumeTopology => _endpointDefinition.ConfigureConsumeTopology;

    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointDefinition.GetEndpointName(formatter);
    }

    public void Configure<T>(T configurator, IRegistrationContext context)
        where T : IReceiveEndpointConfigurator
    {
        _endpointDefinition.Configure(configurator, context ?? _context);
    }

    public bool IsTemporary => _endpointDefinition.IsTemporary;

    public int? PrefetchCount => _endpointDefinition.PrefetchCount;

    public int? ConcurrentMessageLimit => _endpointDefinition.ConcurrentMessageLimit;
}
