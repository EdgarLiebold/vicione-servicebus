namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a settings endpoint definition implementation.
/// </summary>
/// <typeparam name="TSettings">The t settings type.</typeparam>
public abstract class SettingsEndpointDefinition<TSettings> :
    IEndpointDefinition<TSettings>
    where TSettings : class
{
    readonly IEndpointSettings<IEndpointDefinition<TSettings>> _settings;
    string? _endpointName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    protected SettingsEndpointDefinition(IEndpointSettings<IEndpointDefinition<TSettings>> settings)
    {
        _settings = settings;
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
            return string.IsNullOrWhiteSpace(_settings.Name)
                ? FormatEndpointName(formatter)
                : _settings.Name!;
        }

        return _endpointName ??= string.IsNullOrWhiteSpace(_settings.InstanceId)
            ? FormatName()
            : formatter.SanitizeName(FormatName() + formatter.Separator + _settings.InstanceId);
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => _settings.IsTemporary;
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
        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>
    /// Performs the format endpoint name operation.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract string FormatEndpointName(IEndpointNameFormatter formatter);
}
