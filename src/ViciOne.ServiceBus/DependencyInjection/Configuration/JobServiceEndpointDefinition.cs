using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for job service endpoint.</summary>
public class JobServiceEndpointDefinition :
    IEndpointDefinition<JobServiceState>
{
    readonly InstanceJobServiceSettings _jobServiceSettings;
    readonly IEndpointSettings<IEndpointDefinition<JobServiceState>> _settings;
    string? _endpointName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="jobServiceSettings">The job service settings.</param>
    public JobServiceEndpointDefinition(IEndpointSettings<IEndpointDefinition<JobServiceState>> settings, InstanceJobServiceSettings jobServiceSettings)
    {
        _settings = settings;
        _jobServiceSettings = jobServiceSettings;
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => true;
    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => _settings.PrefetchCount;
    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _settings.ConcurrentMessageLimit;
    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => _settings.ConfigureConsumeTopology;

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _jobServiceSettings.ApplyConfiguration(configurator);

        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
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
