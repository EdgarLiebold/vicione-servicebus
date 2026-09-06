namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for settings endpoint.</summary>
/// <typeparam name="TSettings">The settings type.</typeparam>
public abstract class SettingsEndpointDefinition<TSettings> :
    IEndpointDefinition<TSettings>
    where TSettings : class
{
    readonly IEndpointSettings<IEndpointDefinition<TSettings>> _settings;
    string? _endpointName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    protected SettingsEndpointDefinition(IEndpointSettings<IEndpointDefinition<TSettings>> settings)
    {
        _settings = settings;
    }

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        string FormatName()
        {
            return string.IsNullOrWhiteSpace(_settings.Name)
                ? FormatEndpointName(formatter)
                : _settings.Name!;
        }

        return _endpointName ??= string.IsNullOrWhiteSpace(_settings.InstanceId)
            ? FormatName()
            : formatter.SanitizeName(FormatName() + formatter.Separator + _settings.InstanceId);
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => _settings.IsTemporary;
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
        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected abstract string FormatEndpointName(IEndpointNameFormatter formatter);
}
