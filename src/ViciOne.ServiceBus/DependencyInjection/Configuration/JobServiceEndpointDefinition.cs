using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

public class JobServiceEndpointDefinition :
    IEndpointDefinition<JobServiceState>
{
    readonly InstanceJobServiceSettings _jobServiceSettings;
    readonly IEndpointSettings<IEndpointDefinition<JobServiceState>> _settings;
    string? _endpointName;

    public JobServiceEndpointDefinition(IEndpointSettings<IEndpointDefinition<JobServiceState>> settings, InstanceJobServiceSettings jobServiceSettings)
    {
        _settings = settings;
        _jobServiceSettings = jobServiceSettings;
    }

    public bool IsTemporary => true;
    public int? PrefetchCount => _settings.PrefetchCount;
    public int? ConcurrentMessageLimit => _settings.ConcurrentMessageLimit;
    public bool ConfigureConsumeTopology => _settings.ConfigureConsumeTopology;

    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _jobServiceSettings.ApplyConfiguration(configurator);

        _settings.ConfigureEndpoint(configurator, context);
    }

    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        string FormatName()
        {
            return _settings.Name ?? "Instance";
        }

        return _endpointName ??= string.IsNullOrWhiteSpace(_settings.InstanceId)
            ? formatter.SanitizeName(FormatName())
            : formatter.SanitizeName(FormatName() + formatter.Separator + _settings.InstanceId);
    }
}
