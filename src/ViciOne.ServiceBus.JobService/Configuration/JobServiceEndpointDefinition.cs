using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Derives the service-instance endpoint used for local job execution.</summary>
/// <param name="settings">The registered endpoint settings.</param>
/// <param name="jobServiceSettings">The owning job-service instance settings.</param>
internal sealed class JobServiceEndpointDefinition(
    IEndpointSettings<IEndpointDefinition<JobServiceState>> settings,
    InstanceJobServiceSettings jobServiceSettings) :
    IEndpointDefinition<JobServiceState>
{
    readonly InstanceJobServiceSettings _jobServiceSettings =
        jobServiceSettings ?? throw new ArgumentNullException(nameof(jobServiceSettings));
    readonly IEndpointSettings<IEndpointDefinition<JobServiceState>> _settings =
        settings ?? throw new ArgumentNullException(nameof(settings));
    string? _endpointName;

    /// <summary>Gets whether the endpoint is deleted when the service instance disconnects.</summary>
    public bool IsTemporary => true;

    /// <summary>Gets the configured transport prefetch count.</summary>
    public int? PrefetchCount => _settings.PrefetchCount;

    /// <summary>Gets the configured concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _settings.ConcurrentMessageLimit;

    /// <summary>Gets whether the endpoint configures consume topology for its message types.</summary>
    public bool ConfigureConsumeTopology => _settings.ConfigureConsumeTopology;

    /// <summary>Applies endpoint settings and binds the endpoint to the owning job runtime.</summary>
    /// <typeparam name="TConfigurator">The concrete receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The endpoint being configured.</param>
    /// <param name="context">The optional registration context for container-backed configuration.</param>
    public void Configure<TConfigurator>(TConfigurator configurator, IRegistrationContext? context)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        _jobServiceSettings.ApplyConfiguration(configurator);

        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>Returns the stable, sanitized endpoint name for this service instance.</summary>
    /// <param name="formatter">The endpoint-name formatter used by the bus.</param>
    /// <returns>The formatted endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        string FormatName()
        {
            return _settings.Name ?? "Instance";
        }

        return _endpointName ??= string.IsNullOrWhiteSpace(_settings.InstanceId)
            ? formatter.SanitizeName(FormatName())
            : formatter.SanitizeName(FormatName() + formatter.Separator + _settings.InstanceId);
    }
}
