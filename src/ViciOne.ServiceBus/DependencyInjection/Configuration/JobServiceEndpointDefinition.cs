using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job service endpoint definition implementation.
/// </summary>
public class JobServiceEndpointDefinition :
    IEndpointDefinition<JobServiceState>
{
    readonly InstanceJobServiceSettings _jobServiceSettings;
    readonly IEndpointSettings<IEndpointDefinition<JobServiceState>> _settings;
    string? _endpointName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="jobServiceSettings">The job service settings value.</param>
    public JobServiceEndpointDefinition(IEndpointSettings<IEndpointDefinition<JobServiceState>> settings, InstanceJobServiceSettings jobServiceSettings)
    {
        _settings = settings;
        _jobServiceSettings = jobServiceSettings;
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => true;
    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => _settings.PrefetchCount;
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => _settings.ConcurrentMessageLimit;
    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => _settings.ConfigureConsumeTopology;

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        _jobServiceSettings.ApplyConfiguration(configurator);

        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
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
