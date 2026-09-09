using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies stored transport-independent settings to an endpoint definition.</summary>
/// <typeparam name="TRegistration">The consumer, saga, future, or activity registration owned by the endpoint.</typeparam>
public abstract class SettingsEndpointDefinition<TRegistration> :
    IEndpointDefinition<TRegistration>
    where TRegistration : class
{
    readonly IEndpointSettings<IEndpointDefinition<TRegistration>> _settings;
    string? _endpointName;

    /// <summary>Creates a definition backed by the supplied endpoint settings.</summary>
    /// <param name="settings">The transport-independent settings applied by the definition.</param>
    protected SettingsEndpointDefinition(IEndpointSettings<IEndpointDefinition<TRegistration>> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <summary>Gets the configured endpoint name or derives one with the supplied formatter.</summary>
    /// <param name="formatter">The naming convention used when no explicit name is configured.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

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

    /// <summary>Gets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    public bool IsTemporary => _settings.IsTemporary;
    /// <summary>Gets the broker-specific number of messages fetched ahead of processing.</summary>
    public int? PrefetchCount => _settings.PrefetchCount;
    /// <summary>Gets the maximum number of messages processed concurrently on the endpoint.</summary>
    public int? ConcurrentMessageLimit => _settings.ConcurrentMessageLimit;
    /// <summary>Gets whether the transport creates the endpoint's consume topology.</summary>
    public bool ConfigureConsumeTopology => _settings.ConfigureConsumeTopology;

    /// <summary>Applies the stored settings to a transport-specific receive endpoint.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context available to configuration callbacks.</param>
    public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        _settings.ConfigureEndpoint(configurator, context);
    }

    /// <summary>Derives the endpoint's base name from its registration type.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The derived endpoint name.</returns>
    protected abstract string FormatEndpointName(IEndpointNameFormatter formatter);
}
